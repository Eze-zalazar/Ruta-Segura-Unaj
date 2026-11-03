namespace Application.UseCases.Dashboard.ObtenerResumen;

using Application.DTOs;
using Application.Interfaces.Persistence;
using Domain.Enums;

public class ObtenerResumenHandler
{
    private readonly IPedidoRepository _pedidoRepository;

    public ObtenerResumenHandler(IPedidoRepository pedidoRepository)
    {
        _pedidoRepository = pedidoRepository;
    }

    public async Task<DashboardResumenDto> HandleAsync(
        ObtenerResumenQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pedidos = await _pedidoRepository.GetAllAsync(cancellationToken);

        var pedidosPendientes = pedidos.Count(p =>
            p.Estado != EstadoPedido.EnCamino &&
            p.Estado != EstadoPedido.Entregado &&
            p.Estado != EstadoPedido.Cancelado);

        var pedidosEntregados = pedidos.Count(p =>
            p.Estado == EstadoPedido.Entregado);

        var pedidosConInconvenientes = pedidos.Count(p =>
            p.Estado == EstadoPedido.Cancelado);

        return new DashboardResumenDto(
            pedidosPendientes,
            pedidosEntregados,
            pedidosConInconvenientes);
    }
}