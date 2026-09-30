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

    public async Task<UsuarioDto> HandleAsync(LoginUsuarioCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email))
            throw new DomainException("El email es obligatorio para iniciar sesión.");

        if (string.IsNullOrWhiteSpace(command.Password))
            throw new DomainException("La contraseña es obligatoria para iniciar sesión.");

        var emailNormalizado = command.Email.Trim().ToLowerInvariant();
        var usuario = await _usuarioRepository.GetByEmailAsync(emailNormalizado, cancellationToken);
        if (usuario == null)
            throw new DomainException("Credenciales inválidas.");

        var esValida = _passwordHasher.VerifyPassword(command.Password, usuario.PasswordHash);
        if (!esValida)
            throw new DomainException("Credenciales inválidas.");

        return new UsuarioDto(
            usuario.Id,
            usuario.Nombre,
            usuario.Email,
            usuario.Telefono,
            usuario.ObtenerRol());
    }
}
