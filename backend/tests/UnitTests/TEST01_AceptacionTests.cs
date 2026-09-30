namespace UnitTests;

using API.Controllers;
using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.UseCases.Clientes.ActualizarCliente;
using Application.UseCases.Clientes.BuscarClientes;
using Application.UseCases.Clientes.CrearCliente;
using Application.UseCases.Clientes.EliminarCliente;
using Application.UseCases.Clientes.ObtenerClientePorId;
using Application.UseCases.Usuarios.LoginUsuario;
using Domain.Entities;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

public class TEST01_AceptacionTests
{
    // Un único flujo E2E conciso: login -> creación de cliente con sesión -> intento sin sesión
    [Fact]
    public async Task FlujoE2E_LoginConCookie_AltaCliente_EIntentoSinSesion()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("123456");
        var usuario = new Encargado("Silvia", "silvia@correo.com", "1122", hash, "Sur");

        var usuarioRepo = new Mock<IUsuarioRepository>();
        usuarioRepo.Setup(r => r.GetByEmailAsync("silvia@correo.com", default)).ReturnsAsync(usuario);

        var clienteRepo = new Mock<IClienteRepository>();
        var uow = new Mock<IUnitOfWork>();
        var authService = new Mock<IAuthenticationService>();
        var sp = new Mock<IServiceProvider>();
        sp.Setup(s => s.GetService(typeof(IAuthenticationService))).Returns(authService.Object);

        var httpContext = new DefaultHttpContext { RequestServices = sp.Object };

        var authController = new AuthController(new LoginUsuarioHandler(usuarioRepo.Object, hasher))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var clientesController = new ClientesController(
            new BuscarClientesHandler(clienteRepo.Object),
            new ObtenerClientePorIdHandler(clienteRepo.Object),
            new CrearClienteHandler(clienteRepo.Object, uow.Object),
            new ActualizarClienteHandler(clienteRepo.Object, uow.Object),
            new EliminarClienteHandler(clienteRepo.Object, uow.Object))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // 1. Login exitoso con emisión de cookie HttpOnly
        var loginRes = await authController.Login(new LoginRequestDto("silvia@correo.com", "123456"), default);
        Assert.IsType<OkObjectResult>(loginRes.Result);
        authService.Verify(a => a.SignInAsync(httpContext, CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);

        // 2. Creación de cliente con sesión activa
        var altaRes = await clientesController.Create(new CrearClienteCommand("Distribuidora", "1144", "Calchaqui 100"), default);
        Assert.IsType<CreatedAtActionResult>(altaRes);
        clienteRepo.Verify(r => r.AddAsync(It.IsAny<Cliente>(), default), Times.Once);

        // 3. Intento de consulta de sesión sin cookie/autenticación retorna 401 Unauthorized
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        var meRes = authController.GetCurrentUser();
        Assert.IsType<UnauthorizedObjectResult>(meRes.Result);
    }
}
