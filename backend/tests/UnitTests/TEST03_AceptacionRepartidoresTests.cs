namespace UnitTests;

using API.Controllers;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.UseCases.Repartidores.ActualizarRepartidor;
using Application.UseCases.Repartidores.AsignarPedido;
using Application.UseCases.Repartidores.CambiarDisponibilidad;
using Application.UseCases.Repartidores.ConsultarCargaTrabajo;
using Application.UseCases.Repartidores.CrearRepartidor;
using Application.UseCases.Repartidores.ObtenerRepartidores;
using Application.UseCases.Repartidores.ObtenerRepartidorPorId;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

public class TEST03_AceptacionRepartidoresTests
{
    // Un único flujo E2E conciso: Crear repartidor -> Asignar pedido -> Verificar impacto en su carga de trabajo
    [Fact]
    public async Task FlujoE2E_CrearRepartidor_AsignarPedido_YVerificarImpactoCargaTrabajo()
    {
        // 1. Simulación de contexto de Encargado autenticado mediante sesión de cookies
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "Encargado Logística"),
            new Claim(ClaimTypes.Role, "Encargado")
        }, "CookieAuth");

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        // 2. Persistencia en memoria
        var repartidoresDb = new List<Repartidor>();
        var pedidosDb = new List<Pedido>
        {
            new(50, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Alta, "Entrega especial")
        };

        var repartidorRepo = new Mock<IRepartidorRepository>();
        var usuarioRepo = new Mock<IUsuarioRepository>();
        var pedidoRepo = new Mock<IPedidoRepository>();
        var uow = new Mock<IUnitOfWork>();

        usuarioRepo.Setup(u => u.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((Usuario?)null);

        repartidorRepo.Setup(r => r.AddAsync(It.IsAny<Repartidor>(), default))
            .Callback<Repartidor, CancellationToken>((r, _) =>
            {
                typeof(Usuario).GetProperty(nameof(Usuario.Id))?.SetValue(r, 10);
                repartidoresDb.Add(r);
            })
            .Returns(Task.CompletedTask);

        repartidorRepo.Setup(r => r.GetByIdAsync(10, default))
            .ReturnsAsync(() => repartidoresDb.FirstOrDefault(r => r.Id == 10));

        repartidorRepo.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(() => repartidoresDb);

        pedidoRepo.Setup(p => p.GetByIdAsync(50, default))
            .ReturnsAsync(() => pedidosDb.FirstOrDefault(p => p.Id == 50));

        var controller = new RepartidoresController(
            new CrearRepartidorHandler(repartidorRepo.Object, usuarioRepo.Object, uow.Object),
            new ActualizarRepartidorHandler(repartidorRepo.Object, uow.Object),
            new CambiarDisponibilidadHandler(repartidorRepo.Object, uow.Object),
            new ObtenerRepartidoresHandler(repartidorRepo.Object),
            new ObtenerRepartidorPorIdHandler(repartidorRepo.Object),
            new AsignarPedidoHandler(repartidorRepo.Object, pedidoRepo.Object, uow.Object),
            new ConsultarCargaTrabajoHandler(repartidorRepo.Object))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Paso A: Crear repartidor con disponibilidad activa (RF09)
        var cmdCrear = new CrearRepartidorCommand("Gonzalo", "gonzalo@correo.com", "112233", "pass123", "Moto");
        var resCrear = await controller.Create(cmdCrear, default);
        var createdResult = Assert.IsType<CreatedAtActionResult>(resCrear);
        Assert.Equal(10, createdResult.RouteValues?["id"]);

        // Paso B: Asignar pedido #50 al repartidor #10 (RF11)
        var resAsignar = await controller.AsignarPedido(10, 50, default);
        Assert.IsType<NoContentResult>(resAsignar);
        Assert.Equal(EstadoPedido.Asignado, pedidosDb[0].Estado);

        // Paso C: Verificar impacto en su carga de trabajo (RF10)
        var resCarga = await controller.GetCargaTrabajo(null, null, default);
        var okCarga = Assert.IsType<OkObjectResult>(resCarga.Result);
        var cargas = Assert.IsAssignableFrom<IReadOnlyList<CargaRepartidorDto>>(okCarga.Value);
        Assert.Single(cargas);
        Assert.Equal("Gonzalo", cargas[0].Nombre);
        Assert.Equal(1, cargas[0].CargaActiva);
        Assert.True(cargas[0].TieneCapacidad);

        // Verificar impacto en estructura clave-valor
        var resConteo = await controller.GetConteoAsignaciones(default);
        var okConteo = Assert.IsType<OkObjectResult>(resConteo.Result);
        var diccionario = Assert.IsAssignableFrom<Dictionary<string, int>>(okConteo.Value);
        Assert.Equal(1, diccionario["Gonzalo"]);
    }
}
