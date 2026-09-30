namespace API.Controllers;

using Application.DTOs;
using Application.UseCases.Usuarios.LoginUsuario;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginUsuarioHandler _loginHandler;

    public AuthController(LoginUsuarioHandler loginHandler)
    {
        _loginHandler = loginHandler;
    }

    [HttpPost("login")]
    public async Task<ActionResult<UsuarioDto>> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var command = new LoginUsuarioCommand(request.Email, request.Password);
        var usuario = await _loginHandler.HandleAsync(command, ct);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return Ok(usuario);
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { mensaje = "Sesión cerrada correctamente." });
    }

    [HttpGet("me")]
    public ActionResult<UsuarioDto> GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { mensaje = "No hay una sesión activa." });

        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(idStr, out int id);

        var nombre = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        return Ok(new UsuarioDto(id, nombre, email, string.Empty, rol));
    }
}
