namespace UnitTests;

using API.Controllers;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.UseCases.Pedidos.ActualizarPedido;
using Application.UseCases.Pedidos.CancelarPedido;
using Application.UseCases.Pedidos.CrearPedido;
using Application.UseCases.Pedidos.FiltrarPedidos;
using Application.UseCases.Pedidos.ObtenerPedidoPorId;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

public class TEST02_AceptacionPedidosTests
{
    // Un único flujo E2E conciso: Crear pedido autenticado -> Consultarlo en el listado -> Cancelarlo
    [Fact]
    public async Task FlujoE2E_CrearPedidoAutenticado_ConsultarEnListado_YCancelar()
    {
        // 1. Simulación de contexto autenticado mediante sesión con cookies
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "Encargado Prueba"),
            new Claim(ClaimTypes.Role, "Encargado")
        }, "CookieAuth");

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        // 2. Configuración de persistencia compartida
        var pedidosDb = new List<Pedido>();
        var cliente = new Cliente("Kiosco Belgrano", "113344", "Belgrano 500");

        var clienteRepo = new Mock<IClienteRepository>();
        clienteRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(cliente);

        var pedidoRepo = new Mock<IPedidoRepository>();
        var uow = new Mock<IUnitOfWork>();

        pedidoRepo.Setup(r => r.AddAsync(It.IsAny<Pedido>(), default))
            .Callback<Pedido, CancellationToken>((p, _) =>
            {
                // Simula la asignación de ID autoincremental de EF Core
                typeof(Pedido).GetProperty(nameof(Pedido.Id))?.SetValue(p, 101);
                pedidosDb.Add(p);
            })
            .Returns(Task.CompletedTask);

        pedidoRepo.Setup(r => r.GetByIdAsync(101, default))
            .ReturnsAsync(() => pedidosDb.FirstOrDefault(p => p.Id == 101));

        pedidoRepo.Setup(r => r.FiltrarAsync(It.IsAny<EstadoPedido?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), default))
            .ReturnsAsync(() => pedidosDb);

        var controller = new PedidosController(
            new CrearPedidoHandler(pedidoRepo.Object, clienteRepo.Object, uow.Object),
            new ActualizarPedidoHandler(pedidoRepo.Object, uow.Object),
            new CancelarPedidoHandler(pedidoRepo.Object, uow.Object),
            new FiltrarPedidosHandler(pedidoRepo.Object),
            new ObtenerPedidoPorIdHandler(pedidoRepo.Object))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Paso A: Crear pedido autenticado (RF06)
        var cmdCrear = new CrearPedidoCommand(1, DateTime.UtcNow.AddDays(2), PrioridadPedido.Alta, "Entrega urgente de insumos");
        var resCrear = await controller.Create(cmdCrear, default);
        var createdResult = Assert.IsType<CreatedAtActionResult>(resCrear);
        Assert.Equal(101, createdResult.RouteValues?["id"]);

        // Paso B: Consultar pedido en el listado con filtro de cliente (RF08)
        var resListado = await controller.Filtrar(null, null, 1, default);
        var okListado = Assert.IsType<OkObjectResult>(resListado.Result);
        var lista = Assert.IsAssignableFrom<IReadOnlyList<PedidoDto>>(okListado.Value);
        Assert.Single(lista);
        Assert.Equal(101, lista[0].Id);
        Assert.Equal("Entrega urgente de insumos", lista[0].Descripcion);
        Assert.Equal(EstadoPedido.Pendiente, lista[0].Estado);

        // Paso C: Cancelar pedido (RF07)
        var resCancelar = await controller.Cancelar(101, default);
        Assert.IsType<NoContentResult>(resCancelar);

        var pedidoCancelado = pedidosDb.First(p => p.Id == 101);
        Assert.Equal(EstadoPedido.Cancelado, pedidoCancelado.Estado);
    }
}
