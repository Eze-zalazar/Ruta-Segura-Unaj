namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Seguimiento.ConsultarHistorialEntregas;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Xunit;

public class RF14_HistorialEntregasTests
{
    private readonly Mock<IPedidoRepository> _pedidoRepo = new();

    // 1. Camino Feliz: Historial ordenado cronológicamente con última incidencia proyectada
    [Fact]
    public async Task ConsultarHistorial_ConEventosEIncidencias_RetornaOrdenadoCronologicamenteYEventoMasReciente()
    {
        var repartidor = new Repartidor(10, "Repartidor 10", "r10@correo.com", "1122", "pwd", "Moto");
        var fecha1 = new DateTime(2026, 9, 23, 10, 0, 0, DateTimeKind.Utc);
        var fecha2 = new DateTime(2026, 9, 25, 14, 0, 0, DateTimeKind.Utc);

        var p1 = new Pedido(1, 1, fecha1, PrioridadPedido.Media, "Pedido 1");
        var p2 = new Pedido(2, 1, fecha2, PrioridadPedido.Alta, "Pedido 2");

        p1.AsignarRepartidor(repartidor);
        p1.MarcarEnCamino();
        p1.RegistrarIncidencia(new Incidencia(1, 1, "Demora", "Demora en tráfico", new DateTime(2026, 9, 23, 11, 0, 0, DateTimeKind.Utc)));
        p1.RegistrarIncidencia(new Incidencia(2, 1, "DireccionErronea", "Dirección incorrecta", new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc)));

        _pedidoRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<Pedido> { p1, p2 });

        var handler = new ConsultarHistorialEntregasHandler(_pedidoRepo.Object);
        var historial = await handler.HandleAsync(new ConsultarHistorialEntregasQuery());

        Assert.Equal(2, historial.Count);
        // Orden descendente por fecha: p2 primero, luego p1
        Assert.Equal(2, historial[0].PedidoId);
        Assert.Equal(1, historial[1].PedidoId);

        // Última incidencia de p1 debe ser la de las 12:00
        Assert.NotNull(historial[1].UltimaIncidencia);
        Assert.Equal("DireccionErronea", historial[1].UltimaIncidencia!.Tipo);
        Assert.Equal(2, historial[1].Incidencias.Count);
    }

    // 2. Caso Borde: Pedido sin incidencias retorna colección vacía y UltimaIncidencia null
    [Fact]
    public async Task ConsultarHistorial_PedidoSinIncidencias_RetornaColeccionVaciaDeIncidenciasYUltimaIncidenciaNull()
    {
        var repartidor = new Repartidor(10, "Repartidor 10", "r10@correo.com", "1122", "pwd", "Moto");
        var pedido = new Pedido(5, 1, DateTime.UtcNow, PrioridadPedido.Baja, "Pedido limpio");
        pedido.AsignarRepartidor(repartidor);
        pedido.MarcarEnCamino();
        pedido.RegistrarEntrega();

        _pedidoRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<Pedido> { pedido });

        var handler = new ConsultarHistorialEntregasHandler(_pedidoRepo.Object);
        var resultado = await handler.HandleAsync(new ConsultarHistorialEntregasQuery(SoloEntregados: true));

        Assert.Single(resultado);
        var item = resultado[0];
        Assert.Empty(item.Incidencias);
        Assert.Null(item.UltimaIncidencia);
        Assert.Equal(EstadoPedido.Entregado, item.Estado);
    }

    // 3. Verificación de Diccionarios Clave-Valor (LINQ GroupBy + ToDictionary)
    [Fact]
    public async Task ConsultarHistorial_CalculaDiccionariosDeConteo_AgrupaPorTipoIncidenciaYEntregasPorRepartidor()
    {
        var repCarlos = new Repartidor(10, "Carlos", "carlos@correo.com", "123", "pwd", "Moto");
        var repAna = new Repartidor(20, "Ana", "ana@correo.com", "456", "pwd", "Auto");

        var p1 = new Pedido(1, 1, DateTime.UtcNow, PrioridadPedido.Media, "P1");
        p1.AsignarRepartidor(repCarlos);
        p1.MarcarEnCamino();
        p1.RegistrarIncidencia(new Incidencia(1, 1, "Demora", "Demora 1"));
        p1.RegistrarIncidencia(new Incidencia(2, 1, "Demora", "Demora 2"));
        p1.RegistrarEntrega();

        var p2 = new Pedido(2, 1, DateTime.UtcNow, PrioridadPedido.Alta, "P2");
        p2.AsignarRepartidor(repAna);
        p2.MarcarEnCamino();
        p2.RegistrarIncidencia(new Incidencia(3, 2, "Rotura", "Rotura embalaje"));
        p2.RegistrarEntrega();

        _pedidoRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<Pedido> { p1, p2 });

        var handler = new ConsultarHistorialEntregasHandler(_pedidoRepo.Object);

        // Conteo por tipo de incidencia (Dictionary<string, int>)
        var conteoIncidencias = await handler.ObtenerConteoIncidenciasPorTipoAsync();
        Assert.Equal(2, conteoIncidencias["Demora"]);
        Assert.Equal(1, conteoIncidencias["Rotura"]);

        // Conteo por repartidor (Dictionary<string, int>)
        var conteoRepartidores = await handler.ObtenerEntregasConcretadasPorRepartidorAsync();
        Assert.Equal(1, conteoRepartidores["Carlos"]);
        Assert.Equal(1, conteoRepartidores["Ana"]);
    }
}
