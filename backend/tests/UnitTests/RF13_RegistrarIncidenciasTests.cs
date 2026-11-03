namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Seguimiento.RegistrarIncidencia;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Moq;
using Xunit;

public class RF13_RegistrarIncidenciasTests
{
    private readonly Mock<IPedidoRepository> _pedidoRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // 1. Camino Feliz: Registro válido de incidencia en pedido en camino
    [Fact]
    public async Task RegistrarIncidencia_DatosValidosEnPedidoEnCurso_AgregaIncidenciaYDevuelveDto()
    {
        var repartidor = new Repartidor(3, "Lucas", "lucas@correo.com", "1122", "pwd", "Moto");
        var pedido = new Pedido(20, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Alta, "Carga delicada");
        pedido.AsignarRepartidor(repartidor);
        pedido.MarcarEnCamino();

        _pedidoRepo.Setup(p => p.GetByIdAsync(20, default)).ReturnsAsync(pedido);

        var handler = new RegistrarIncidenciaHandler(_pedidoRepo.Object, _uow.Object);
        var command = new RegistrarIncidenciaCommand(20, "Demora", "Tráfico pesado por corte en Av. Calchaquí");
        var dto = await handler.HandleAsync(command);

        Assert.NotNull(dto);
        Assert.Equal("Demora", dto.Tipo);
        Assert.Equal("Tráfico pesado por corte en Av. Calchaquí", dto.Descripcion);
        Assert.Single(pedido.Incidencias);
        _pedidoRepo.Verify(p => p.Update(pedido), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 2. Caso Borde: Descripción en longitud mínima permitida (5 caracteres)
    [Fact]
    public async Task RegistrarIncidencia_DescripcionEnLongitudMinimaValida_RegistraIncidenciaExitosamente()
    {
        var repartidor = new Repartidor(2, "Martin", "martin@correo.com", "1133", "pwd", "Auto");
        var pedido = new Pedido(21, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Caja chica");
        pedido.AsignarRepartidor(repartidor);
        pedido.MarcarEnCamino();

        _pedidoRepo.Setup(p => p.GetByIdAsync(21, default)).ReturnsAsync(pedido);

        var handler = new RegistrarIncidenciaHandler(_pedidoRepo.Object, _uow.Object);
        var command = new RegistrarIncidenciaCommand(21, "Otro", "Lluvia"); // 6 caracteres
        var dto = await handler.HandleAsync(command);

        Assert.Equal("Lluvia", dto.Descripcion);
        Assert.Single(pedido.Incidencias);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 3. Caso Error: Registro de incidencia en pedido ya finalizado (Entregado)
    [Fact]
    public async Task RegistrarIncidencia_SobrePedidoEntregado_LanzaDomainException()
    {
        var repartidor = new Repartidor(4, "Diego", "diego@correo.com", "1144", "pwd", "Camión");
        var pedido = new Pedido(22, 1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Baja, "Paquete finalizado");
        pedido.AsignarRepartidor(repartidor);
        pedido.MarcarEnCamino();
        pedido.RegistrarEntrega();

        _pedidoRepo.Setup(p => p.GetByIdAsync(22, default)).ReturnsAsync(pedido);

        var handler = new RegistrarIncidenciaHandler(_pedidoRepo.Object, _uow.Object);
        var command = new RegistrarIncidenciaCommand(22, "Rotura", "Caja con signos de rotura");

        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Contains("entregado", ex.Message, StringComparison.OrdinalIgnoreCase);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
