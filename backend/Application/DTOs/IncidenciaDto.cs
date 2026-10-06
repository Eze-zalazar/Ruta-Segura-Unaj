namespace Application.DTOs;

public record IncidenciaDto(
    int Id,
    int PedidoId,
    string Tipo,
    string Descripcion,
    DateTime FechaHora,
    bool Resuelta);
