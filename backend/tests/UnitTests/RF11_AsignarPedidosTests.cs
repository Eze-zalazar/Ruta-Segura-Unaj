namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Repartidores.AsignarPedido;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Moq;
using Xunit;

public class RF11_AsignarPedidosTests
{
    private readonly Mock<IRepartidorRepository> _repartidorRepo = new();
    private readonly Mock<IPedidoRepository> _pedidoRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // 1. Camino Feliz: Asignación exitosa con cambio de estado
    [Fact]
    public async Task AsignarPedido_RepartidorDisponibleYPedidoPendiente_VinculaYCambiaEstadoAAsignado()
    {
        var repartidor = new Repartidor(1, "Lucas", "lucas@correo.com", "1122", "pwd", "Moto");
        var pedido = new Pedido(10, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Alta, "Caja de repuestos");

        _repartidorRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(repartidor);
        _pedidoRepo.Setup(r => r.GetByIdAsync(10, default)).ReturnsAsync(pedido);

        var handler = new AsignarPedidoHandler(_repartidorRepo.Object, _pedidoRepo.Object, _uow.Object);
        await handler.HandleAsync(new AsignarPedidoCommand(1, 10));

        Assert.Equal(EstadoPedido.Asignado, pedido.Estado);
        Assert.Equal(1, pedido.RepartidorId);
        Assert.Contains(pedido, repartidor.Pedidos);
        _repartidorRepo.Verify(r => r.Update(repartidor), Times.Once);
        _pedidoRepo.Verify(p => p.Update(pedido), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 2. Caso Borde: Repartidor en el límite exacto de capacidad máxima bloquea asignación
    [Fact]
    public async Task AsignarPedido_RepartidorEnCapacidadMaxima_LanzaDomainException()
    {
        // Repartidor con capacidad máxima de 2
        var repartidor = new Repartidor(1, "Marcos", "marcos@correo.com", "1133", "pwd", "Auto", disponible: true, capacidadMaxima: 2);
        repartidor.AsignarPedido(new Pedido(1, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "P1"));
        repartidor.AsignarPedido(new Pedido(2, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "P2"));

        var pedido3 = new Pedido(3, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "P3");

        _repartidorRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(repartidor);
        _pedidoRepo.Setup(r => r.GetByIdAsync(3, default)).ReturnsAsync(pedido3);

        var handler = new AsignarPedidoHandler(_repartidorRepo.Object, _pedidoRepo.Object, _uow.Object);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new AsignarPedidoCommand(1, 3)));
        Assert.Contains("capacidad máxima", ex.Message);
    }

    // 3. Caso Error: Repartidor no disponible o pedido ya entregado
    [Fact]
    public async Task AsignarPedido_RepartidorNoDisponibleOPedidoEntregado_LanzaDomainException()
    {
        var repartidorInactivo = new Repartidor(1, "Sofia", "sofia@correo.com", "1144", "pwd", "Bici", disponible: false);
        var pedidoPendiente = new Pedido(10, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "P10");

        var repartidorActivo = new Repartidor(2, "Tomas", "tomas@correo.com", "1155", "pwd", "Moto", disponible: true);
        var pedidoEntregado = new Pedido(20, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "P20", null, EstadoPedido.Entregado);

        _repartidorRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(repartidorInactivo);
        _repartidorRepo.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync(repartidorActivo);
        _pedidoRepo.Setup(r => r.GetByIdAsync(10, default)).ReturnsAsync(pedidoPendiente);
        _pedidoRepo.Setup(r => r.GetByIdAsync(20, default)).ReturnsAsync(pedidoEntregado);

        var handler = new AsignarPedidoHandler(_repartidorRepo.Object, _pedidoRepo.Object, _uow.Object);

        // Repartidor no disponible
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new AsignarPedidoCommand(1, 10)));

        // Pedido ya entregado
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new AsignarPedidoCommand(2, 20)));
    }
}
