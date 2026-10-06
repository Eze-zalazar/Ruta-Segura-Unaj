
namespace Application.DTOs;

public record DashboardResumenDto(
    int PedidosPendientes,
    int PedidosEntregados,
    int PedidosConInconvenientes);