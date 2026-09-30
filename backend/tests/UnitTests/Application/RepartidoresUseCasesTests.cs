namespace UnitTests.Application;

using global::Application.Interfaces.Persistence;
using global::Application.UseCases.Repartidores.ActualizarRepartidor;
using global::Application.UseCases.Repartidores.CambiarDisponibilidad;
using global::Application.UseCases.Repartidores.CrearRepartidor;
using global::Application.UseCases.Repartidores.ObtenerRepartidores;
using global::Domain.Entities;
using global::Domain.Exceptions;
using Moq;
using Xunit;

public class RepartidoresUseCasesTests
{
    private readonly Mock<IRepartidorRepository> _repartidorRepoMock = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();

    // 1. CrearRepartidor - Caso Feliz
    [Fact]
    public async Task CrearRepartidor_ConDatosValidos_DebePersistirYDevolverId()
    {
        // Arrange
        _usuarioRepoMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var handler = new CrearRepartidorHandler(_repartidorRepoMock.Object, _usuarioRepoMock.Object, _uowMock.Object);
        var command = new CrearRepartidorCommand("Jorge Repartidor", "jorge@correo.com", "1133445566", "123456", "Camioneta Berlingo");

        // Act
        var id = await handler.HandleAsync(command);

        // Assert
        _repartidorRepoMock.Verify(r => r.AddAsync(It.IsAny<Repartidor>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // 2. CrearRepartidor - Caso de Error (Email duplicado)
    [Fact]
    public async Task CrearRepartidor_ConEmailExistente_DebeLanzarDomainException()
    {
        // Arrange
        var usuarioExistente = new Repartidor("Otro", "jorge@correo.com", "1111", "pwd", "Moto");
        _usuarioRepoMock
            .Setup(r => r.GetByEmailAsync("jorge@correo.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarioExistente);

        var handler = new CrearRepartidorHandler(_repartidorRepoMock.Object, _usuarioRepoMock.Object, _uowMock.Object);
        var command = new CrearRepartidorCommand("Jorge", "jorge@correo.com", "1133445566", "123456", "Moto");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Contains("Ya existe un usuario", ex.Message);
        _repartidorRepoMock.Verify(r => r.AddAsync(It.IsAny<Repartidor>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // 3. ActualizarRepartidor - Caso Normal
    [Fact]
    public async Task ActualizarRepartidor_ConRepartidorExistente_DebeModificarDatos()
    {
        // Arrange
        var repartidor = new Repartidor("Mario", "mario@correo.com", "1122", "pwd", "Moto");
        _repartidorRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repartidor);

        var handler = new ActualizarRepartidorHandler(_repartidorRepoMock.Object, _uowMock.Object);
        var command = new ActualizarRepartidorCommand(1, "Mario Alberto", "1133", "Auto Fiat");

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Equal("Mario Alberto", repartidor.Nombre);
        Assert.Equal("Auto Fiat", repartidor.Vehiculo);
        _repartidorRepoMock.Verify(r => r.Update(repartidor), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // 4. ActualizarRepartidor - Caso de Error (No encontrado)
    [Fact]
    public async Task ActualizarRepartidor_NoExistente_DebeLanzarDomainException()
    {
        // Arrange
        _repartidorRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Repartidor?)null);

        var handler = new ActualizarRepartidorHandler(_repartidorRepoMock.Object, _uowMock.Object);
        var command = new ActualizarRepartidorCommand(999, "Mario", "1122", "Moto");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Contains("No se encontró ningún repartidor", ex.Message);
    }

    // 5. CambiarDisponibilidad - Caso Feliz
    [Fact]
    public async Task CambiarDisponibilidad_DebeActualizarEstadoYPersistir()
    {
        // Arrange
        var repartidor = new Repartidor("Mario", "mario@correo.com", "1122", "pwd", "Moto", disponible: true);
        _repartidorRepoMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repartidor);

        var handler = new CambiarDisponibilidadHandler(_repartidorRepoMock.Object, _uowMock.Object);
        var command = new CambiarDisponibilidadCommand(1, false);

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.False(repartidor.Disponible);
        _repartidorRepoMock.Verify(r => r.Update(repartidor), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // 6. ObtenerRepartidores - Caso Feliz y Filtro
    [Fact]
    public async Task ObtenerRepartidores_ConFiltroSoloDisponibles_DebeInvocarMetodoCorrespondiente()
    {
        // Arrange
        var lista = new List<Repartidor>
        {
            new("Mario", "mario@correo.com", "1122", "pwd", "Moto", disponible: true)
        };

        _repartidorRepoMock
            .Setup(r => r.GetDisponiblesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(lista);

        var handler = new ObtenerRepartidoresHandler(_repartidorRepoMock.Object);

        // Act
        var resultado = await handler.HandleAsync(new ObtenerRepartidoresQuery(SoloDisponibles: true));

        // Assert
        Assert.Single(resultado);
        Assert.Equal("Mario", resultado[0].Nombre);
        _repartidorRepoMock.Verify(r => r.GetDisponiblesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
