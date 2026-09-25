namespace UnitTests.Application;

using global::Application.Interfaces.Persistence;
using global::Application.UseCases.Clientes.BuscarClientes;
using global::Domain.Entities;
using Moq;
using Xunit;

public class BuscarClientesUseCaseTests
{
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();

    [Fact]
    public async Task BuscarClientes_ConTermino_DebeFiltrarYDevolverDtos()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            new("Kiosco Central", "11223344", "Av. San Martín 100", "Frente a la plaza")
        };

        _clienteRepoMock
            .Setup(r => r.BuscarAsync("Central", It.IsAny<CancellationToken>()))
            .ReturnsAsync(clientes);

        var handler = new BuscarClientesHandler(_clienteRepoMock.Object);

        // Act
        var resultado = await handler.HandleAsync(new BuscarClientesQuery("Central"));

        // Assert
        Assert.Single(resultado);
        Assert.Equal("Kiosco Central", resultado[0].Nombre);
        Assert.Equal("Av. San Martín 100", resultado[0].Direccion);
        _clienteRepoMock.Verify(r => r.BuscarAsync("Central", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuscarClientes_SinTermino_DebeLlamarBuscarConNullYDevolverTodos()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            new("Cliente A", "111", "Dir A"),
            new("Cliente B", "222", "Dir B")
        };

        _clienteRepoMock
            .Setup(r => r.BuscarAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clientes);

        var handler = new BuscarClientesHandler(_clienteRepoMock.Object);

        // Act
        var resultado = await handler.HandleAsync(new BuscarClientesQuery(null));

        // Assert
        Assert.Equal(2, resultado.Count);
        _clienteRepoMock.Verify(r => r.BuscarAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
