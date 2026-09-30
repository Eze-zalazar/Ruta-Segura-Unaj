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
/// TEST-01: Pruebas de Integración y Aceptación de Extremo a Extremo.
/// Cubre los 2 flujos críticos de Definition of Done (DoD) para Usuarios y Clientes.
/// </summary>
public class TEST01_AceptacionTests
{
    private readonly PasswordHasher _hasher = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock = new();
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IAuthenticationService> _authServiceMock = new();

    private readonly List<Usuario> _usuariosStore = new();
    private readonly List<Cliente> _clientesStore = new();

    public TEST01_AceptacionTests()
    {
        _usuarioRepoMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string email, CancellationToken _) =>
                _usuariosStore.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)));

        _clienteRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
            .Callback<Cliente, CancellationToken>((c, _) => _clientesStore.Add(c))
            .Returns(Task.CompletedTask);

        _clienteRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => _clientesStore.FirstOrDefault(c => c.Id == id));

        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private (AuthController authController, ClientesController clientesController) CreateControllers()
    {
        var loginHandler = new LoginUsuarioHandler(_usuarioRepoMock.Object, _hasher);
        var authController = new AuthController(loginHandler);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
            .Returns(_authServiceMock.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProviderMock.Object
        };

        authController.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var clientesController = new ClientesController(
            new BuscarClientesHandler(_clienteRepoMock.Object),
            new ObtenerClientePorIdHandler(_clienteRepoMock.Object),
            new CrearClienteHandler(_clienteRepoMock.Object, _uowMock.Object),
            new ActualizarClienteHandler(_clienteRepoMock.Object, _uowMock.Object),
            new EliminarClienteHandler(_clienteRepoMock.Object, _uowMock.Object))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        return (authController, clientesController);
    }

    // 1. Flujo E2E Crítico 1 (Happy Path Completo de Extremo a Extremo)
    [Fact]
    public async Task FlujoE2E_LoginConCookie_AltaDeClienteConSesionActiva_YConsultaExitosa()
    {
        // GIVEN: Usuario Encargado registrado en el sistema
        var pwd = "EncargadoSeguro2026!";
        var encargado = new Encargado("Silvia Gomez", "silvia@rutasegura.com", "11223344", _hasher.HashPassword(pwd), "Sector Sur");
        _usuariosStore.Add(encargado);

        var (authController, clientesController) = CreateControllers();

        // PASO 1: Iniciar sesión exitosamente y emitir cookie HttpOnly
        var loginReq = new LoginRequestDto("silvia@rutasegura.com", pwd);
        var loginRes = await authController.Login(loginReq, CancellationToken.None);
        var okLogin = Assert.IsType<OkObjectResult>(loginRes.Result);
        var userDto = Assert.IsType<UsuarioDto>(okLogin.Value);
        Assert.Equal("Encargado", userDto.Rol);

        _authServiceMock.Verify(a => a.SignInAsync(
            authController.HttpContext,
            CookieAuthenticationDefaults.AuthenticationScheme,
            It.Is<ClaimsPrincipal>(p => p.HasClaim(ClaimTypes.Role, "Encargado")),
            It.IsAny<AuthenticationProperties>()), Times.Once);

        // PASO 2: Alta de un nuevo cliente con la sesión activa
        var altaCmd = new CrearClienteCommand("Distribuidora Quilmes SRL", "1144556677", "Av. Calchaquí 5000", "Portón 3");
        var altaRes = await clientesController.Create(altaCmd, CancellationToken.None);
        Assert.IsType<CreatedAtActionResult>(altaRes);

        var clientePersistido = Assert.Single(_clientesStore);
        Assert.Equal("Distribuidora Quilmes SRL", clientePersistido.Nombre);

        // PASO 3: Consultar el cliente creado por ID
        var getRes = await clientesController.GetById(clientePersistido.Id, CancellationToken.None);
        var okGet = Assert.IsType<OkObjectResult>(getRes.Result);
        var clienteDto = Assert.IsType<ClienteDto>(okGet.Value);
        Assert.Equal("Distribuidora Quilmes SRL", clienteDto.Nombre);
        Assert.Equal("1144556677", clienteDto.Telefono);
    }

    // 2. Flujo E2E Crítico 2 (Rechazo de Acceso No Autorizado y Credenciales Erradas)
    [Fact]
    public async Task FlujoE2E_OperacionSinSesionValida_OCredencialesErroneas_RechazaAcceso()
    {
        var (authController, _) = CreateControllers();

        // CASO A: Intento de login con credenciales erróneas es rechazado sin emitir sesión
        var loginErroneo = new LoginRequestDto("noexiste@correo.com", "ClaveInvalida");
        var ex = await Assert.ThrowsAsync<DomainException>(() => authController.Login(loginErroneo, CancellationToken.None));
        Assert.Equal("Credenciales inválidas.", ex.Message);

        _authServiceMock.Verify(a => a.SignInAsync(
            It.IsAny<HttpContext>(),
            It.IsAny<string>(),
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<AuthenticationProperties>()), Times.Never);

        // CASO B: Solicitud de usuario actual sin cookie/sesión autenticada devuelve 401 Unauthorized
        authController.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Sin autenticar
        var meRes = authController.GetCurrentUser();
        Assert.IsType<UnauthorizedObjectResult>(meRes.Result);
    }
}
