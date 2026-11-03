namespace Application.DTOs;

using Domain.Enums;

public record HistorialEntregaDto(
    int PedidoId,
    string Descripcion,
    EstadoPedido Estado,
    DateTime FechaPactada,
    int? RepartidorId,
    string? RepartidorNombre,
    string? ClienteNombre,
    IReadOnlyList<IncidenciaDto> Incidencias,
    IncidenciaDto? UltimaIncidencia);
