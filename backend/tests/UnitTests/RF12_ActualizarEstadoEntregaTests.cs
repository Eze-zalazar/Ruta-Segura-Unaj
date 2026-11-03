namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Seguimiento.ActualizarEstadoEntrega;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Moq;
using Xunit;

public class RF12_ActualizarEstadoEntregaTests
{
    private readonly Mock<IPedidoRepository> _pedidoRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // 1. Camino Feliz: Transición válida a Entregado desde EnCamino
    [Fact]
    public async Task ActualizarEstadoEntrega_DeEnCaminoAEntregado_TransicionaExitosamente()
    {
        var repartidor = new Repartidor(5, "Repartidor 5", "r5@correo.com", "1122", "pwd", "Moto");
        var pedido = new Pedido(10, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Mercadería frágil");
        pedido.AsignarRepartidor(repartidor);
        pedido.MarcarEnCamino();

        _pedidoRepo.Setup(p => p.GetByIdAsync(10, default)).ReturnsAsync(pedido);

        var handler = new ActualizarEstadoEntregaHandler(_pedidoRepo.Object, _uow.Object);
        await handler.HandleAsync(new ActualizarEstadoEntregaCommand(10, EstadoPedido.Entregado));

        Assert.Equal(EstadoPedido.Entregado, pedido.Estado);
        _pedidoRepo.Verify(p => p.Update(pedido), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 2. Caso Borde: Transición al mismo estado previo (idempotencia)
    [Fact]
    public async Task ActualizarEstadoEntrega_MismoEstadoPrevio_MantieneEstadoSinError()
    {
        var repartidor = new Repartidor(5, "Repartidor 5", "r5@correo.com", "1122", "pwd", "Moto");
        var pedido = new Pedido(11, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Baja, "Paquete estándar");
        pedido.AsignarRepartidor(repartidor);
        pedido.MarcarEnCamino();

        _pedidoRepo.Setup(p => p.GetByIdAsync(11, default)).ReturnsAsync(pedido);

        var handler = new ActualizarEstadoEntregaHandler(_pedidoRepo.Object, _uow.Object);
        await handler.HandleAsync(new ActualizarEstadoEntregaCommand(11, EstadoPedido.EnCamino));

        Assert.Equal(EstadoPedido.EnCamino, pedido.Estado);
        _pedidoRepo.Verify(p => p.Update(pedido), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    // 3. Caso Error: Transición inválida sobre pedido en estado terminal (Cancelado)
    [Fact]
    public async Task ActualizarEstadoEntrega_SobrePedidoCancelado_LanzaDomainException()
    {
        var pedido = new Pedido(12, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Alta, "Documentación");
        pedido.Cancelar();

        _pedidoRepo.Setup(p => p.GetByIdAsync(12, default)).ReturnsAsync(pedido);

        var handler = new ActualizarEstadoEntregaHandler(_pedidoRepo.Object, _uow.Object);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new ActualizarEstadoEntregaCommand(12, EstadoPedido.EnCamino)));

        Assert.Contains("cancelado", ex.Message, StringComparison.OrdinalIgnoreCase);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
