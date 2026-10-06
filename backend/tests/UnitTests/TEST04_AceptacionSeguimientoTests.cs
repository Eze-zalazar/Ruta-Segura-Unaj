namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Repartidores.AsignarPedido;
using Application.UseCases.Seguimiento.ActualizarEstadoEntrega;
using Application.UseCases.Seguimiento.ConsultarHistorialEntregas;
using Application.UseCases.Seguimiento.RegistrarIncidencia;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Xunit;

public class TEST04_AceptacionSeguimientoTests
{
    // Test de integración E2E: Asignar -> En Camino -> Registrar Incidencia -> Entregar
    [Fact]
    public async Task FlujoE2E_AsignarRepartidor_MarcarEnCamino_RegistrarIncidencia_YEntregarConTrazabilidad()
    {
        // 1. Configuración de repositorio y UoW en memoria
        var repartidor = new Repartidor(1, "Federico Rossi", "fede@correo.com", "11223344", "hash123", "Furgoneta");
        var pedido = new Pedido(100, 10, DateTime.UtcNow.AddHours(2), PrioridadPedido.Alta, "Insumos hospitalarios");

        var pedidoRepo = new Mock<IPedidoRepository>();
        var repartidorRepo = new Mock<IRepartidorRepository>();
        var uow = new Mock<IUnitOfWork>();

        pedidoRepo.Setup(p => p.GetByIdAsync(100, default)).ReturnsAsync(pedido);
        repartidorRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(repartidor);
        pedidoRepo.Setup(p => p.GetAllAsync(default)).ReturnsAsync(new List<Pedido> { pedido });

        // 2. Paso 1: Asignar pedido a repartidor (RF11)
        var asignarHandler = new AsignarPedidoHandler(repartidorRepo.Object, pedidoRepo.Object, uow.Object);
        await asignarHandler.HandleAsync(new AsignarPedidoCommand(1, 100));

        Assert.Equal(EstadoPedido.Asignado, pedido.Estado);
        Assert.Equal(1, pedido.RepartidorId);

        // 3. Paso 2: Marcar pedido en camino (RF12)
        var actualizarEstadoHandler = new ActualizarEstadoEntregaHandler(pedidoRepo.Object, uow.Object);
        await actualizarEstadoHandler.HandleAsync(new ActualizarEstadoEntregaCommand(100, EstadoPedido.EnCamino));

        Assert.Equal(EstadoPedido.EnCamino, pedido.Estado);

        // 4. Paso 3: Registrar incidencia durante el trayecto (RF13)
        var registrarIncidenciaHandler = new RegistrarIncidenciaHandler(pedidoRepo.Object, uow.Object);
        var dtoIncidencia = await registrarIncidenciaHandler.HandleAsync(
            new RegistrarIncidenciaCommand(100, "Demora", "Corte total en autopista por obras"));

        Assert.NotNull(dtoIncidencia);
        Assert.Equal("Demora", dtoIncidencia.Tipo);
        Assert.Single(pedido.Incidencias);

        // 5. Paso 4: Finalizar entrega exitosamente (RF12)
        await actualizarEstadoHandler.HandleAsync(new ActualizarEstadoEntregaCommand(100, EstadoPedido.Entregado));

        Assert.Equal(EstadoPedido.Entregado, pedido.Estado);

        // 6. Paso 5: Consultar trazabilidad en el Historial (RF14)
        var historialHandler = new ConsultarHistorialEntregasHandler(pedidoRepo.Object);
        var historial = await historialHandler.HandleAsync(new ConsultarHistorialEntregasQuery(SoloEntregados: true));

        Assert.Single(historial);
        var trazabilidad = historial[0];
        Assert.Equal(100, trazabilidad.PedidoId);
        Assert.Equal(EstadoPedido.Entregado, trazabilidad.Estado);
        Assert.Equal("Federico Rossi", trazabilidad.RepartidorNombre);
        Assert.Single(trazabilidad.Incidencias);
        Assert.NotNull(trazabilidad.UltimaIncidencia);
        Assert.Equal("Demora", trazabilidad.UltimaIncidencia!.Tipo);

        // Verificamos llamadas de persistencia del ciclo completo
        uow.Verify(u => u.SaveChangesAsync(default), Times.AtLeast(4));
    }
}
