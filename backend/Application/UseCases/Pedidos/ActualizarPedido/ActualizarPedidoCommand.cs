namespace Application.UseCases.Pedidos.ActualizarPedido;

using Domain.Enums;

public record ActualizarPedidoCommand(
    int Id,
    string Descripcion,
    DateTime FechaPactada,
    PrioridadPedido Prioridad,
    string? Observaciones = null);
