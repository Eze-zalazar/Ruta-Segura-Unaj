namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Clientes.BuscarClientes;
using Domain.Entities;
using Moq;
using Xunit;

/// <summary>
/// RF05: Búsqueda y filtro de clientes (Lógica existente preservada).
/// Regla de 3 pruebas: Camino Normal, Caso de Borde y Caso de Error / Sin coincidencias.
/// </summary>
public class RF05_BusquedaClientesTests
{
    private readonly List<Cliente> _dataset = new()
    {
        new("Kiosco San Martín", "1144332211", "Av. San Martín 1500"),
        new("Farmacia Central", "1199887766", "Calle Belgrano 230"),
        new("Distribuidora Quilmes", "1122334455", "Av. Calchaquí 6200")
    };

    private Mock<IClienteRepository> CreateMockRepository()
    {
        var mock = new Mock<IClienteRepository>();
        mock.Setup(r => r.BuscarAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? termino, CancellationToken _) =>
            {
                if (string.IsNullOrWhiteSpace(termino))
                    return _dataset;

                var term = termino.Trim().ToLower();
                return _dataset.Where(c =>
                    c.Nombre.ToLower().Contains(term) ||
                    c.Direccion.ToLower().Contains(term) ||
                    c.Telefono.Contains(term)).ToList();
            });

        return mock;
    }

    // a) Camino Normal (Happy Path: Filtro parcial multi-campo e insensible a mayúsculas)
    [Fact]
    public async Task BuscarClientes_PorTerminoParcial_DebeFiltrarPorNombreODireccion()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var handler = new BuscarClientesHandler(mockRepo.Object);

        // Act: Búsqueda en minúsculas sobre nombre con mayúsculas
        var resultadoNombre = await handler.HandleAsync(new BuscarClientesQuery("san martín"));
        var resultadoDireccion = await handler.HandleAsync(new BuscarClientesQuery("belgrano"));

        // Assert
        Assert.Single(resultadoNombre);
        Assert.Equal("Kiosco San Martín", resultadoNombre[0].Nombre);

        Assert.Single(resultadoDireccion);
        Assert.Equal("Farmacia Central", resultadoDireccion[0].Nombre);
    }

    // b) Caso de Borde (Término nulo o en blanco devuelve catálogo completo)
    [Fact]
    public async Task BuscarClientes_ConTerminoNuloOEspacios_DebeRetornarCatalogoCompleto()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var handler = new BuscarClientesHandler(mockRepo.Object);

        // Act
        var resNull = await handler.HandleAsync(new BuscarClientesQuery(null));
        var resEspacios = await handler.HandleAsync(new BuscarClientesQuery("   "));

        // Assert: Ambas peticiones deben devolver los 3 clientes sin truncar
        Assert.Equal(3, resNull.Count);
        Assert.Equal(3, resEspacios.Count);
    }

    // c) Caso de Error / Sin Coincidencias (Término inexistente retorna lista vacía sin fallar)
    [Fact]
    public async Task BuscarClientes_SinCoincidencias_DebeRetornarListaVaciaSinErrores()
    {
        // Arrange
        var mockRepo = CreateMockRepository();
        var handler = new BuscarClientesHandler(mockRepo.Object);

        // Act
        var resultado = await handler.HandleAsync(new BuscarClientesQuery("TerminoInexistente_XYZ"));

        // Assert
        Assert.NotNull(resultado);
        Assert.Empty(resultado);
    }
}
