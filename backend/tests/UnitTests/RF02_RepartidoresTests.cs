namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Repartidores.ActualizarRepartidor;
using Application.UseCases.Repartidores.CambiarDisponibilidad;
using Application.UseCases.Repartidores.CrearRepartidor;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Moq;
using Xunit;

/// <summary>
/// RF02: ABM usuarios repartidores (Lógica existente preservada).
/// Regla de 3 pruebas: Camino Normal, Caso de Borde y Caso de Error.
/// </summary>
public class RF02_RepartidoresTests
{
    private readonly Mock<IRepartidorRepository> _repartidorRepoMock = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();

    // a) Camino Normal (Happy Path)
    [Fact]
    public async Task ABMRepartidor_AltaYModificacionValida_DebePersistirCambios()
    {
        // Arrange: Crear repartidor
        _usuarioRepoMock
            .Setup(r => r.GetByEmailAsync("roberto@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var crearHandler = new CrearRepartidorHandler(_repartidorRepoMock.Object, _usuarioRepoMock.Object, _uowMock.Object);
        var crearCmd = new CrearRepartidorCommand("Roberto Repartidor", "roberto@correo.com", "11223344", "clave123", "Furgón Renault");

        await crearHandler.HandleAsync(crearCmd);

        _repartidorRepoMock.Verify(r => r.AddAsync(It.IsAny<Repartidor>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Arrange & Act: Actualizar repartidor existente
        var repartidorExistente = new Repartidor("Roberto Repartidor", "roberto@correo.com", "11223344", "hash", "Furgón Renault");
        _repartidorRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repartidorExistente);

        var actualizarHandler = new ActualizarRepartidorHandler(_repartidorRepoMock.Object, _uowMock.Object);
        var actCmd = new ActualizarRepartidorCommand(1, "Roberto Carlos", "11998877", "Camioneta Berlingo");

        await actualizarHandler.HandleAsync(actCmd);

        // Assert
        Assert.Equal("Roberto Carlos", repartidorExistente.Nombre);
        Assert.Equal("11998877", repartidorExistente.Telefono);
        Assert.Equal("Camioneta Berlingo", repartidorExistente.Vehiculo);
        _repartidorRepoMock.Verify(r => r.Update(repartidorExistente), Times.Once);
    }

    // b) Caso de Borde (Restricción de negocio por disponibilidad)
    [Fact]
    public async Task CambiarDisponibilidad_CuandoPasaANoDisponible_DebeBloquearAsignacionDeEntregas()
    {
        // Arrange
        var repartidor = new Repartidor("Lucas Ruiz", "lucas@correo.com", "11554433", "pwd", "Moto", disponible: true);
        _repartidorRepoMock
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repartidor);

        var dispHandler = new CambiarDisponibilidadHandler(_repartidorRepoMock.Object, _uowMock.Object);

        // Act: Cambiar a no disponible
        await dispHandler.HandleAsync(new CambiarDisponibilidadCommand(10, false));

        // Assert: Valida que el estado cambió y que la regla de dominio prohíbe asignarle entregas
        Assert.False(repartidor.Disponible);
        Assert.False(repartidor.PuedeAsignarseEntregas());

        var pedido = new Pedido(1, DateTime.UtcNow.AddHours(2), PrioridadPedido.Media, "Paquete estándar");
        var ex = Assert.Throws<DomainException>(() => repartidor.AgregarPedido(pedido));
        Assert.Contains("no está disponible", ex.Message);
    }

    // c) Caso de Error / Excepción (Email duplicado o password vacío)
    [Fact]
    public async Task CrearRepartidor_ConEmailDuplicadoOPasswordVacio_DebeLanzarDomainException()
    {
        // Arrange: Email duplicado
        var usuarioExistente = new Repartidor("Otro", "duplicado@correo.com", "1122", "pwd", "Moto");
        _usuarioRepoMock
            .Setup(r => r.GetByEmailAsync("duplicado@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarioExistente);

        var handler = new CrearRepartidorHandler(_repartidorRepoMock.Object, _usuarioRepoMock.Object, _uowMock.Object);

        // Act & Assert 1: Email duplicado
        var cmdDuplicado = new CrearRepartidorCommand("Jorge", "duplicado@correo.com", "113344", "123456", "Moto");
        var ex1 = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(cmdDuplicado));
        Assert.Contains("Ya existe un usuario", ex1.Message);

        // Act & Assert 2: Contraseña vacía
        var cmdPasswordVacio = new CrearRepartidorCommand("Jorge", "nuevo@correo.com", "113344", "   ", "Moto");
        var ex2 = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(cmdPasswordVacio));
        Assert.Contains("contraseña no puede estar vacía", ex2.Message);
    }
}
