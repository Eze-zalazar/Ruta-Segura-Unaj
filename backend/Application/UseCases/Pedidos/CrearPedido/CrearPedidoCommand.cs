namespace Application.UseCases.Pedidos.CrearPedido;

using Domain.Enums;

public record CrearPedidoCommand(
    int ClienteId,
    DateTime FechaPactada,
    PrioridadPedido Prioridad,
    string Descripcion,
    string? Observaciones = null);
