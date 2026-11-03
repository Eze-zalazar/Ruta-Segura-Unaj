namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Pedidos.ActualizarPedido;
using Application.UseCases.Pedidos.CancelarPedido;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Moq;
using Xunit;

public class RF07_EdicionCancelacionPedidosTests
{
    private readonly Mock<IPedidoRepository> _pedidoRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // 1. Camino Feliz: Cancelación exitosa cambiando el estado del pedido
    [Fact]
    public async Task CancelarPedido_PedidoPendiente_CambiaEstadoACancelado()
    {
        var pedido = new Pedido(1, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Caja de herramientas");
        _pedidoRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(pedido);

        var handler = new CancelarPedidoHandler(_pedidoRepo.Object, _uow.Object);
        await handler.HandleAsync(new CancelarPedidoCommand(1));

        Assert.Equal(EstadoPedido.Cancelado, pedido.Estado);
        _pedidoRepo.Verify(r => r.Update(pedido), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 2. Camino Feliz (Edición): Modificación de campos permitidos
    [Fact]
    public async Task ActualizarPedido_PedidoPendiente_ActualizaCamposPermitidos()
    {
        var pedido = new Pedido(1, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Baja, "Original", "Obs vieja");
        _pedidoRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(pedido);

        var handler = new ActualizarPedidoHandler(_pedidoRepo.Object, _uow.Object);
        var nuevaFecha = DateTime.UtcNow.AddDays(3);
        await handler.HandleAsync(new ActualizarPedidoCommand(1, "Modificado", nuevaFecha, PrioridadPedido.Urgente, "Obs nueva"));

        Assert.Equal("Modificado", pedido.Descripcion);
        Assert.Equal(nuevaFecha, pedido.FechaPactada);
        Assert.Equal(PrioridadPedido.Urgente, pedido.Prioridad);
        Assert.Equal("Obs nueva", pedido.Observaciones);
        _pedidoRepo.Verify(r => r.Update(pedido), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 3. Caso Error / Borde: Intento de cancelar o editar pedido entregado o inexistente
    [Fact]
    public async Task CancelarOEditar_PedidoEntregadoOInexistente_LanzaDomainException()
    {
        var pedidoEntregado = new Pedido(1, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Entregado", null, EstadoPedido.Entregado);
        _pedidoRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(pedidoEntregado);
        _pedidoRepo.Setup(r => r.GetByIdAsync(999, default)).ReturnsAsync((Pedido?)null);

        var cancelarHandler = new CancelarPedidoHandler(_pedidoRepo.Object, _uow.Object);
        var actualizarHandler = new ActualizarPedidoHandler(_pedidoRepo.Object, _uow.Object);

        // Cancelar pedido ya entregado
        await Assert.ThrowsAsync<DomainException>(() =>
            cancelarHandler.HandleAsync(new CancelarPedidoCommand(1)));

        // Modificar pedido ya entregado
        await Assert.ThrowsAsync<DomainException>(() =>
            actualizarHandler.HandleAsync(new ActualizarPedidoCommand(1, "Nuevo", DateTime.UtcNow.AddDays(2), PrioridadPedido.Alta)));

        // Cancelar pedido inexistente
        await Assert.ThrowsAsync<DomainException>(() =>
            cancelarHandler.HandleAsync(new CancelarPedidoCommand(999)));

        // Modificar pedido inexistente
        await Assert.ThrowsAsync<DomainException>(() =>
            actualizarHandler.HandleAsync(new ActualizarPedidoCommand(999, "Nuevo", DateTime.UtcNow.AddDays(2), PrioridadPedido.Alta)));
    }
}
