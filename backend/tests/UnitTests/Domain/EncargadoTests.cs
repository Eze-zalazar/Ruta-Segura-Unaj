namespace UnitTests.Domain;

using global::Domain.Entities;
using global::Domain.Exceptions;
using Xunit;

public class EncargadoTests
{
    // 1. Caso Normal (Camino feliz y polimorfismo)
    [Fact]
    public void Constructor_ConParametrosValidos_DebeCrearEncargadoCorrectamente()
    {
        // Arrange & Act
        var encargado = new Encargado("Marta Díaz", "marta@correo.com", "1188990011", "hash_pass", "Logística Central");

        // Assert
        Assert.Equal("Marta Díaz", encargado.Nombre);
        Assert.Equal("marta@correo.com", encargado.Email);
        Assert.Equal("Logística Central", encargado.Sector);
        Assert.Equal("Encargado", encargado.ObtenerRol());
        Assert.False(encargado.PuedeAsignarseEntregas());
    }

    [Fact]
    public void ActualizarSector_ConNuevoSectorValido_DebeModificarAtributo()
    {
        // Arrange
        var encargado = new Encargado("Marta Díaz", "marta@correo.com", "1188990011", "hash_pass", "Sector Norte");

        // Act
        encargado.ActualizarSector("Sector Sur");

        // Assert
        Assert.Equal("Sector Sur", encargado.Sector);
    }

    // 2. Caso de Error (Excepciones de negocio controladas)
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_ConSectorInvalido_DebeLanzarDomainException(string? sectorInvalido)
    {
        var ex = Assert.Throws<DomainException>(() =>
            new Encargado("Marta Díaz", "marta@correo.com", "1188990011", "hash_pass", sectorInvalido!));

        Assert.Contains("sector del encargado no puede estar vacío", ex.Message);
    }

    // 3. Caso Borde (Normalización de espacios)
    [Fact]
    public void Constructor_ConEspaciosAlrededor_DebeHacerTrimEnSector()
    {
        // Arrange & Act
        var encargado = new Encargado("  Marta Díaz  ", "marta@correo.com", "1188990011", "hash_pass", "  Despacho 1  ");

        // Assert
        Assert.Equal("Marta Díaz", encargado.Nombre);
        Assert.Equal("Despacho 1", encargado.Sector);
    }
}
