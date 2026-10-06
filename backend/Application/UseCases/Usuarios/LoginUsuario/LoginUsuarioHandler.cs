namespace Application.UseCases.Usuarios.LoginUsuario;

using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Exceptions;

public class LoginUsuarioHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;

    public LoginUsuarioHandler(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UsuarioDto> HandleAsync(
        LoginUsuarioCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        ValidarDatos(command);

        var email = command.Email.Trim();
        var usuario = await _usuarioRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (usuario is null ||
            !_passwordHasher.VerifyPassword(
                command.Password,
                usuario.PasswordHash))
        {
            throw new DomainException("Credenciales inválidas.");
        }

        return CrearUsuarioDto(usuario);
    }

    private static void ValidarDatos(LoginUsuarioCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            throw new DomainException(
                "El email es obligatorio para iniciar sesión.");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            throw new DomainException(
                "La contraseña es obligatoria para iniciar sesión.");
        }
    }

    private static UsuarioDto CrearUsuarioDto(dynamic usuario)
    {
        return new UsuarioDto(
            usuario.Id,
            usuario.Nombre,
            usuario.Email,
            usuario.Telefono,
            usuario.ObtenerRol());
    }
}
