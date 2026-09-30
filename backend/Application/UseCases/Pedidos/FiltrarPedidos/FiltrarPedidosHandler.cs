namespace Application.UseCases.Pedidos.FiltrarPedidos;

using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Mappings;

public class FiltrarPedidosHandler
{
    private readonly IPedidoRepository _pedidoRepository;

    public FiltrarPedidosHandler(IPedidoRepository pedidoRepository)
    {
        _pedidoRepository = pedidoRepository;
    }

    public async Task<IReadOnlyList<PedidoDto>> HandleAsync(FiltrarPedidosQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new FiltrarPedidosQuery();

        var pedidos = await _pedidoRepository.FiltrarAsync(query.Estado, query.Fecha, query.ClienteId, cancellationToken);

        // Recorridos LINQ explícitos: ordenamiento por fecha pactada y mapeo a DTOs
        return pedidos
            .OrderBy(p => p.FechaPactada)
            .Select(p => p.ToDto())
            .ToList();
    }
}
