
namespace Application.UseCases.Usuarios.LoginUsuario;

/// <summary>
/// Representa la solicitud para iniciar sesión de un usuario.
/// </summary>
/// <param name="Email">Correo electrónico del usuario.</param>
/// <param name="Password">Contraseña del usuario.</param>
public record LoginUsuarioCommand(
    string Email,
    string Password
);