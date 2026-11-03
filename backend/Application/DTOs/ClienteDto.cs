namespace Application.DTOs;

public record ClienteDto(
    int Id,
    string Nombre,
    string Telefono,
    string Direccion,
    string? Referencia);
