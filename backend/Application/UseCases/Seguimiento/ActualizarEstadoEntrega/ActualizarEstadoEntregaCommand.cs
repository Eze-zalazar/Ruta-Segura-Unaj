namespace Application.UseCases.Seguimiento.ActualizarEstadoEntrega;

using Domain.Enums;

public record ActualizarEstadoEntregaCommand(int PedidoId, EstadoPedido NuevoEstado);
