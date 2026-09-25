namespace UnitTests.Domain;

using global::Domain.Entities;
using global::Domain.Exceptions;
using Xunit;

public class ClienteTests
{
    // 1. Caso Normal (Camino feliz)
    [Fact]
    public void Constructor_ConParametrosValidos_DebeCrearClienteCorrectamente()
    {
        // Arrange & Act
        var cliente = new Cliente("Empresa San Juan", "1122334455", "Av. Calchaquí 6200", "Frente al hipermercado");

        // Assert
        Assert.Equal("Empresa San Juan", cliente.Nombre);
        Assert.Equal("1122334455", cliente.Telefono);
        Assert.Equal("Av. Calchaquí 6200", cliente.Direccion);
        Assert.Equal("Frente al hipermercado", cliente.Referencia);
        Assert.Empty(cliente.Pedidos);
    }

    [Fact]
    public void ActualizarContacto_ConDatosValidos_DebeModificarTelefonoYDireccion()
    {
        // Arrange
        var cliente = new Cliente("Comercio Central", "1144445555", "San Martín 100");

        // Act
        cliente.ActualizarContacto("1199998888", "Mitre 250", "Entre España e Italia");

        // Assert
        Assert.Equal("1199998888", cliente.Telefono);
        Assert.Equal("Mitre 250", cliente.Direccion);
        Assert.Equal("Entre España e Italia", cliente.Referencia);
    }

    // 2. Caso de Error (Excepciones de negocio controladas)
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_ConNombreInvalido_DebeLanzarDomainException(string? nombreInvalido)
    {
        var ex = Assert.Throws<DomainException>(() =>
            new Cliente(nombreInvalido!, "1122334455", "Calle Falsa 123"));

        Assert.Contains("nombre del cliente no puede estar vacío", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_ConTelefonoInvalido_DebeLanzarDomainException(string? telefonoInvalido)
    {
        var ex = Assert.Throws<DomainException>(() =>
            new Cliente("Juan Perez", telefonoInvalido!, "Calle Falsa 123"));

        Assert.Contains("teléfono del cliente no puede estar vacío", ex.Message);
    }

    // 3. Caso Borde (Normalización de espacios y referencias opcionales vacías)
    [Fact]
    public void Constructor_ConEspaciosAlrededor_DebeHacerTrim()
    {
        // Arrange & Act
        var cliente = new Cliente("  Librería Mitre  ", "  1133221100  ", "  Av. Belgrano 450  ", "  Local 3  ");

        // Assert
        Assert.Equal("Librería Mitre", cliente.Nombre);
        Assert.Equal("1133221100", cliente.Telefono);
        Assert.Equal("Av. Belgrano 450", cliente.Direccion);
        Assert.Equal("Local 3", cliente.Referencia);
    }

    [Fact]
    public void Constructor_ConReferenciaVacia_DebeNormalizarANull()
    {
        // Arrange & Act
        var cliente = new Cliente("Kiosco 24hs", "1122334455", "Peatonal 400", "   ");

        // Assert
        Assert.Null(cliente.Referencia);
    }
}
