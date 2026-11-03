namespace Application.UseCases.Pedidos.ObtenerPedidoPorId;

using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Mappings;

public class ObtenerPedidoPorIdHandler
{
    private readonly IPedidoRepository _pedidoRepository;

    public ObtenerPedidoPorIdHandler(IPedidoRepository pedidoRepository)
    {
        _pedidoRepository = pedidoRepository;
    }

    public async Task<PedidoDto?> HandleAsync(ObtenerPedidoPorIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pedido = await _pedidoRepository.GetByIdAsync(query.Id, cancellationToken);
        return pedido?.ToDto();
    }
}
