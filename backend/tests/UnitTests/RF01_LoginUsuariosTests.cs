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

public class RF01_LoginUsuariosTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepo = new();
    private readonly PasswordHasher _hasher = new();

    // 1. Camino Feliz: Login con emisión de cookie HttpOnly
    [Fact]
    public async Task Login_CredencialesValidas_EmiteCookieHttpOnlyYRetornaUsuario()
    {
        var hash = _hasher.HashPassword("123456");
        var usuario = new Encargado("Ana", "ana@correo.com", "112233", hash, "Logistica");
        _usuarioRepo.Setup(r => r.GetByEmailAsync("ana@correo.com", default)).ReturnsAsync(usuario);

        var authService = new Mock<IAuthenticationService>();
        var sp = new Mock<IServiceProvider>();
        sp.Setup(s => s.GetService(typeof(IAuthenticationService))).Returns(authService.Object);

        var controller = new AuthController(new LoginUsuarioHandler(_usuarioRepo.Object, _hasher))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = sp.Object } }
        };

        var result = await controller.Login(new LoginRequestDto("ana@correo.com", "123456"), default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<UsuarioDto>(ok.Value);
        Assert.Equal("Encargado", dto.Rol);
        authService.Verify(a => a.SignInAsync(controller.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    // 2. Caso Borde: Espacios en blanco antes/después del email
    [Fact]
    public async Task Login_EmailConEspacios_NormalizaYAutentica()
    {
        var hash = _hasher.HashPassword("123456");
        var repartidor = new Repartidor("Carlos", "carlos@correo.com", "1122", hash, "Moto");
        _usuarioRepo.Setup(r => r.GetByEmailAsync("carlos@correo.com", default)).ReturnsAsync(repartidor);

        var handler = new LoginUsuarioHandler(_usuarioRepo.Object, _hasher);
        var result = await handler.HandleAsync(new LoginUsuarioCommand("   carlos@correo.com   ", "123456"));

        Assert.Equal("carlos@correo.com", result.Email);
        _usuarioRepo.Verify(r => r.GetByEmailAsync("carlos@correo.com", default), Times.Once);
    }

    // 3. Caso Error: Credenciales incorrectas
    [Fact]
    public async Task Login_CredencialesIncorrectas_LanzaDomainException()
    {
        _usuarioRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((Usuario?)null);
        var handler = new LoginUsuarioHandler(_usuarioRepo.Object, _hasher);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new LoginUsuarioCommand("error@correo.com", "pwd")));
        Assert.Equal("Credenciales inválidas.", ex.Message);
    }
}
