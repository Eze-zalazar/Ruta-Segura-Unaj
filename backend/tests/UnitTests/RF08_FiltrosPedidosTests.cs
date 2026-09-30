namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Pedidos.FiltrarPedidos;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Xunit;

public class RF08_FiltrosPedidosTests
{
    private readonly Mock<IPedidoRepository> _pedidoRepo = new();

    // 1. Camino Feliz: Retorno de pedidos ordenados por fecha pactada
    [Fact]
    public async Task FiltrarPedidos_RetornaPedidosOrdenadosPorFechaPactada()
    {
        var fecha1 = new DateTime(2026, 9, 5, 10, 0, 0, DateTimeKind.Utc);
        var fecha2 = new DateTime(2026, 9, 3, 14, 0, 0, DateTimeKind.Utc);
        var fecha3 = new DateTime(2026, 9, 7, 9, 0, 0, DateTimeKind.Utc);

        // Lista intencionalmente desordenada para validar que el handler aplique OrderBy(p => p.FechaPactada)
        var pedidos = new List<Pedido>
        {
            new(1, 1, fecha1, PrioridadPedido.Media, "Pedido intermedio"),
            new(2, 1, fecha2, PrioridadPedido.Alta, "Pedido más próximo"),
            new(3, 1, fecha3, PrioridadPedido.Baja, "Pedido más lejano")
        };

        _pedidoRepo.Setup(r => r.FiltrarAsync(It.IsAny<EstadoPedido?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), default))
            .ReturnsAsync(pedidos);

        var handler = new FiltrarPedidosHandler(_pedidoRepo.Object);
        var result = await handler.HandleAsync(new FiltrarPedidosQuery());

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].Id); // fecha2 (3 Sep)
        Assert.Equal(1, result[1].Id); // fecha1 (5 Sep)
        Assert.Equal(3, result[2].Id); // fecha3 (7 Sep)
        Assert.True(result[0].FechaPactada <= result[1].FechaPactada);
        Assert.True(result[1].FechaPactada <= result[2].FechaPactada);
    }

    // 2. Caso Borde: Búsqueda con filtros sin coincidencias retorna colección vacía, no null
    [Fact]
    public async Task FiltrarPedidos_SinCoincidencias_RetornaColeccionVaciaNoNull()
    {
        _pedidoRepo.Setup(r => r.FiltrarAsync(EstadoPedido.Cancelado, null, null, default))
            .ReturnsAsync(new List<Pedido>());

        var handler = new FiltrarPedidosHandler(_pedidoRepo.Object);
        var result = await handler.HandleAsync(new FiltrarPedidosQuery(EstadoPedido.Cancelado));

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
