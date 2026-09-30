namespace UnitTests;

using API.Controllers;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.UseCases.Usuarios.LoginUsuario;
using Domain.Entities;
using Domain.Exceptions;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

/// <summary>
/// RF01: Login de usuarios (autenticación y sesiones HttpOnly).
/// Regla de 3 pruebas: Camino Normal, Caso de Borde y Caso de Error.
/// </summary>
public class RF01_LoginUsuariosTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock = new();
    private readonly PasswordHasher _hasher = new();
    private readonly Mock<IAuthenticationService> _authServiceMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();

    private AuthController CreateController()
    {
        var handler = new LoginUsuarioHandler(_usuarioRepoMock.Object, _hasher);
        var controller = new AuthController(handler);

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
            .Returns(_authServiceMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProviderMock.Object
            }
        };

        return controller;
    }

    // a) Camino Normal (Happy Path)
    [Fact]
    public async Task Login_ConCredencialesValidas_DebeAutenticarEmitirCookieHttpOnlyYRetornarUsuario()
    {
        // Arrange
        var passwordPlano = "Admin2026!";
        var passwordHash = _hasher.HashPassword(passwordPlano);
        var encargado = new Encargado("Ana Supervisora", "ana@rutasegura.com", "11223344", passwordHash, "Depósito Central");

        _usuarioRepoMock
            .Setup(r => r.GetByEmailAsync("ana@rutasegura.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(encargado);

        var controller = CreateController();
        var request = new LoginRequestDto("ana@rutasegura.com", passwordPlano);

        // Act
        var result = await controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UsuarioDto>(okResult.Value);
        Assert.Equal("Ana Supervisora", dto.Nombre);
        Assert.Equal("Encargado", dto.Rol);

        // Verifica emisión de la cookie de sesión nativa HttpOnly con los claims correctos
        _authServiceMock.Verify(a => a.SignInAsync(
            controller.HttpContext,
            CookieAuthenticationDefaults.AuthenticationScheme,
            It.Is<ClaimsPrincipal>(p => p.HasClaim(ClaimTypes.Role, "Encargado") && p.HasClaim(ClaimTypes.Email, "ana@rutasegura.com")),
            It.Is<AuthenticationProperties>(props => props.IsPersistent)), Times.Once);
    }

    // b) Caso de Borde (Límites, compatibilidad y normalización)
    [Fact]
    public async Task Login_ConEspaciosEnBlancoYFormatoLegacy_DebeNormalizarYValidarCorrectamente()
    {
        // Arrange: usuario con hash legacy en Base64 y email enviado con espacios adicionales
        var plainPassword = "ClaveLegacy123";
        var legacyHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainPassword));
        var repartidor = new Repartidor("Carlos Gómez", "carlos@correo.com", "11445566", legacyHash, "Moto");

        _usuarioRepoMock
            .Setup(r => r.GetByEmailAsync("carlos@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(repartidor);

        var handler = new LoginUsuarioHandler(_usuarioRepoMock.Object, _hasher);
        var command = new LoginUsuarioCommand("   carlos@correo.com   ", plainPassword);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert: Valida retrocompatibilidad de hash, resolución polimórfica de rol y sanitización de email
        Assert.NotNull(result);
        Assert.Equal("Repartidor", result.Rol);
        Assert.Equal("Carlos Gómez", result.Nombre);
        _usuarioRepoMock.Verify(r => r.GetByEmailAsync("carlos@correo.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    // c) Caso de Error / Excepción
    [Fact]
    public async Task Login_ConCredencialesInvalidas_DebeRechazarConDomainException()
    {
        // Arrange
        var passwordHash = _hasher.HashPassword("PasswordCorrecto");
        var usuario = new Repartidor("Mario", "mario@correo.com", "112233", passwordHash, "Auto");

        _usuarioRepoMock
            .Setup(r => r.GetByEmailAsync("mario@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var handler = new LoginUsuarioHandler(_usuarioRepoMock.Object, _hasher);
        var commandConPasswordErroneo = new LoginUsuarioCommand("mario@correo.com", "PasswordErroneo");

        // Act & Assert: Contraseña incorrecta
        var ex1 = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(commandConPasswordErroneo));
        Assert.Equal("Credenciales inválidas.", ex1.Message);

        // Act & Assert: Usuario inexistente
        var commandUsuarioInexistente = new LoginUsuarioCommand("desconocido@correo.com", "Cualquiera123");
        var ex2 = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(commandUsuarioInexistente));
        Assert.Equal("Credenciales inválidas.", ex2.Message);
    }
}
