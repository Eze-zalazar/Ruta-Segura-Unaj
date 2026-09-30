namespace Application.DTOs;

public record UsuarioDto(
    int Id,
    string Nombre,
    string Email,
    string Telefono,
    string Rol);
