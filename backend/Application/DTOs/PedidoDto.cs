namespace Application.DTOs;

using Domain.Enums;

public record PedidoDto(
    int Id,
    int ClienteId,
    string? ClienteNombre,
    DateTime FechaCreacion,
    DateTime FechaPactada,
    PrioridadPedido Prioridad,
    EstadoPedido Estado,
    string Descripcion,
    string? Observaciones,
    int? RepartidorId,
    string? RepartidorNombre);
