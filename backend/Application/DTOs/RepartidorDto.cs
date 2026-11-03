namespace Application.DTOs;

public record RepartidorDto(
    int Id,
    string Nombre,
    string Email,
    string Telefono,
    string Vehiculo,
    bool Disponible);
