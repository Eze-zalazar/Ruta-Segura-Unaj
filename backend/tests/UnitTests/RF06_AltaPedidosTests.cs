namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Pedidos.CrearPedido;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Moq;
using Xunit;

public class RF06_AltaPedidosTests
{
    private readonly Mock<IPedidoRepository> _pedidoRepo = new();
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // 1. Camino Feliz: Alta exitosa con datos válidos y cliente existente
    [Fact]
    public async Task CrearPedido_DatosValidosYClienteExistente_PersisteYRetornaId()
    {
        var cliente = new Cliente("Kiosco Sol", "112233", "Av. San Martín 100");
        _clienteRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(cliente);

        var handler = new CrearPedidoHandler(_pedidoRepo.Object, _clienteRepo.Object, _uow.Object);
        var cmd = new CrearPedidoCommand(1, DateTime.UtcNow.AddDays(2), PrioridadPedido.Alta, "2 cajas de bebidas", "Entregar en puerta");

        await handler.HandleAsync(cmd);

        _pedidoRepo.Verify(r => r.AddAsync(It.IsAny<Pedido>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 2. Caso Borde: Pedido con campos opcionales vacíos o espacios antes/después
    [Fact]
    public async Task CrearPedido_CamposOpcionalesVacios_NormalizaYPersisteCorrectamente()
    {
        var cliente = new Cliente("Librería Mitre", "119988", "Mitre 200");
        _clienteRepo.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync(cliente);

        Pedido? pedidoGuardado = null;
        _pedidoRepo.Setup(r => r.AddAsync(It.IsAny<Pedido>(), default))
            .Callback<Pedido, CancellationToken>((p, _) => pedidoGuardado = p)
            .Returns(Task.CompletedTask);

        var handler = new CrearPedidoHandler(_pedidoRepo.Object, _clienteRepo.Object, _uow.Object);
        var cmd = new CrearPedidoCommand(2, DateTime.UtcNow.AddDays(1), PrioridadPedido.Baja, "   Resmas de papel   ", "   ");

        await handler.HandleAsync(cmd);

        Assert.NotNull(pedidoGuardado);
        Assert.Equal("Resmas de papel", pedidoGuardado.Descripcion);
        Assert.Null(pedidoGuardado.Observaciones);
        Assert.Equal(EstadoPedido.Pendiente, pedidoGuardado.Estado);
    }

    // 3. Caso Error: Cliente inexistente o datos inválidos
    [Fact]
    public async Task CrearPedido_ClienteInexistenteODatosInvalidos_LanzaDomainException()
    {
        _clienteRepo.Setup(r => r.GetByIdAsync(999, default)).ReturnsAsync((Cliente?)null);

        var handler = new CrearPedidoHandler(_pedidoRepo.Object, _clienteRepo.Object, _uow.Object);

        // Cliente no registrado
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CrearPedidoCommand(999, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Artículos")));

        // Cliente ID inválido (<= 0)
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CrearPedidoCommand(0, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Artículos")));

        // Descripción vacía con cliente válido
        var cliente = new Cliente("Farmacia Central", "114455", "Rivadavia 50");
        _clienteRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(cliente);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CrearPedidoCommand(1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "   ")));
    }
}
