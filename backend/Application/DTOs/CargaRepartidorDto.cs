namespace Application.DTOs;

public record CargaRepartidorDto(
    int Id,
    string Nombre,
    string Vehiculo,
    bool Disponible,
    int CargaActiva,
    int CapacidadMaxima,
    bool TieneCapacidad);
