namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Clientes.BuscarClientes;
using Domain.Entities;
using Moq;
using Xunit;

public class RF05_BusquedaClientesTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();

    // 1. Camino Feliz: Búsqueda por coincidencia
    [Fact]
    public async Task BuscarClientes_PorCoincidencia_RetornaFiltrados()
    {
        var lista = new List<Cliente> { new("Kiosco San Martín", "1122", "San Martín 100") };
        _clienteRepo.Setup(r => r.BuscarAsync("San Martín", default)).ReturnsAsync(lista);

        var handler = new BuscarClientesHandler(_clienteRepo.Object);
        var result = await handler.HandleAsync(new BuscarClientesQuery("San Martín"));

        Assert.Single(result);
        Assert.Equal("Kiosco San Martín", result[0].Nombre);
    }

    // 2. Caso Sin Resultados: Colección vacía
    [Fact]
    public async Task BuscarClientes_SinCoincidencias_RetornaColeccionVacia()
    {
        _clienteRepo.Setup(r => r.BuscarAsync("Inexistente", default)).ReturnsAsync(new List<Cliente>());

        var handler = new BuscarClientesHandler(_clienteRepo.Object);
        var result = await handler.HandleAsync(new BuscarClientesQuery("Inexistente"));

        Assert.Empty(result);
    }
}
