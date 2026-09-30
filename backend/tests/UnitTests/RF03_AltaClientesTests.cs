namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Clientes.CrearCliente;
using Domain.Entities;
using Domain.Exceptions;
using Moq;
using Xunit;

/// <summary>
/// RF03: Alta de clientes.
/// Regla de 3 pruebas: Camino Normal, Caso de Borde y Caso de Error.
/// </summary>
public class RF03_AltaClientesTests
{
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();

    // a) Camino Normal (Happy Path)
    [Fact]
    public async Task CrearCliente_ConDatosValidos_DebePersistirYRetornarId()
    {
        // Arrange
        Cliente? clientePersistido = null;
        _clienteRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
            .Callback<Cliente, CancellationToken>((c, _) => clientePersistido = c)
            .Returns(Task.CompletedTask);

        var handler = new CrearClienteHandler(_clienteRepoMock.Object, _uowMock.Object);
        var command = new CrearClienteCommand("Distribuidora El Triunfo", "1144332211", "Av. San Martín 1500", "Galpón 2");

        // Act
        var id = await handler.HandleAsync(command);

        // Assert
        Assert.NotNull(clientePersistido);
        Assert.Equal("Distribuidora El Triunfo", clientePersistido.Nombre);
        Assert.Equal("1144332211", clientePersistido.Telefono);
        Assert.Equal("Av. San Martín 1500", clientePersistido.Direccion);
        Assert.Equal("Galpón 2", clientePersistido.Referencia);

        _clienteRepoMock.Verify(r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // b) Caso de Borde (Trimming de espacios y referencia opcional en blanco)
    [Fact]
    public async Task CrearCliente_ConEspaciosYReferenciaOpcionalVacia_DebeNormalizarCorrectamente()
    {
        // Arrange
        Cliente? clientePersistido = null;
        _clienteRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
            .Callback<Cliente, CancellationToken>((c, _) => clientePersistido = c)
            .Returns(Task.CompletedTask);

        var handler = new CrearClienteHandler(_clienteRepoMock.Object, _uowMock.Object);
        var command = new CrearClienteCommand("  Librería Mitre  ", "  11998877  ", "  Belgrano 450  ", "   ");

        // Act
        await handler.HandleAsync(command);

        // Assert: Valida recorte de espacios y normalización a null para referencias vacías
        Assert.NotNull(clientePersistido);
        Assert.Equal("Librería Mitre", clientePersistido.Nombre);
        Assert.Equal("11998877", clientePersistido.Telefono);
        Assert.Equal("Belgrano 450", clientePersistido.Direccion);
        Assert.Null(clientePersistido.Referencia);
    }

    // c) Caso de Error / Excepción (Validación de campos obligatorios)
    [Fact]
    public async Task CrearCliente_ConCamposObligatoriosFaltantes_DebeLanzarDomainException()
    {
        var handler = new CrearClienteHandler(_clienteRepoMock.Object, _uowMock.Object);

        // Nombre vacío
        var exNombre = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CrearClienteCommand("", "112233", "Direccion Valida")));
        Assert.Contains("nombre del cliente no puede estar vacío", exNombre.Message);

        // Teléfono vacío
        var exTel = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CrearClienteCommand("Cliente Valido", "   ", "Direccion Valida")));
        Assert.Contains("teléfono del cliente no puede estar vacío", exTel.Message);

        // Dirección vacía
        var exDir = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CrearClienteCommand("Cliente Valido", "112233", "")));
        Assert.Contains("dirección del cliente no puede estar vacía", exDir.Message);

        _clienteRepoMock.Verify(r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
