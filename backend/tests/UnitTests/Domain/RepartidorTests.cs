namespace UnitTests.Domain;

using global::Domain.Entities;
using global::Domain.Enums;
using global::Domain.Exceptions;
using Xunit;

public class RepartidorTests
{
    // 1. Caso Normal (Camino feliz y polimorfismo)
    [Fact]
    public void Constructor_ConParametrosValidos_DebeCrearRepartidorYCumplirRol()
    {
        // Arrange & Act
        var repartidor = new Repartidor("Carlos Gómez", "carlos@correo.com", "1166778899", "hashed_pwd", "Moto Honda 150");

        // Assert
        Assert.Equal("Carlos Gómez", repartidor.Nombre);
        Assert.Equal("carlos@correo.com", repartidor.Email);
        Assert.Equal("Moto Honda 150", repartidor.Vehiculo);
        Assert.True(repartidor.Disponible);
        Assert.Equal("Repartidor", repartidor.ObtenerRol());
        Assert.True(repartidor.PuedeAsignarseEntregas());
    }

    [Fact]
    public void SetDisponible_CuandoPasaAFalso_NoDebePoderAsignarseEntregas()
    {
        // Arrange
        var repartidor = new Repartidor("Carlos Gómez", "carlos@correo.com", "1166778899", "pwd", "Moto");

        // Act
        repartidor.SetDisponible(false);

        // Assert
        Assert.False(repartidor.Disponible);
        Assert.False(repartidor.PuedeAsignarseEntregas());
    }

    // 2. Caso de Error (Excepciones de negocio controladas)
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_ConVehiculoInvalido_DebeLanzarDomainException(string? vehiculoInvalido)
    {
        var ex = Assert.Throws<DomainException>(() =>
            new Repartidor("Lucas Ruiz", "lucas@correo.com", "1155443322", "pwd", vehiculoInvalido!));

        Assert.Contains("vehículo del repartidor no puede estar vacío", ex.Message);
    }

    [Fact]
    public void AgregarPedido_CuandoRepartidorNoEstaDisponible_DebeLanzarDomainException()
    {
        // Arrange
        var repartidor = new Repartidor("Lucas Ruiz", "lucas@correo.com", "1155443322", "pwd", "Bicicleta", disponible: false);
        var pedido = new Pedido(1, DateTime.UtcNow.AddHours(2), PrioridadPedido.Media, "Caja de herramientas");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => repartidor.AgregarPedido(pedido));
        Assert.Contains("no está disponible", ex.Message);
    }

    // 3. Caso Borde (Colección vacía)
    [Fact]
    public void Recorridos_SobreColeccionVacia_NoDebenLanzarExcepcionYSonConsistentes()
    {
        // Arrange
        var repartidor = new Repartidor("Esteban", "esteban@correo.com", "11223344", "pwd", "Auto");

        // Act & Assert
        Assert.Empty(repartidor.ObtenerPedidosEntregados());
        Assert.Empty(repartidor.ObtenerPedidosNoEntregados());
        Assert.Empty(repartidor.ObtenerResumenPedidos());
        Assert.Null(repartidor.BuscarPrimerPedidoUrgente());
        Assert.Empty(repartidor.ConteoPedidosPorEstado());
        Assert.Empty(repartidor.ObtenerPedidosOrdenadosPorFechaPactada());
    }

    // 4. Pruebas de los 4 Recorridos Obligatorios + Estructura Clave-Valor + Ordenamiento
    [Fact]
    public void RecorridosYDiccionario_ConVariosPedidos_DebenOperarCorrectamente()
    {
        // Arrange
        var repartidor = new Repartidor("Esteban", "esteban@correo.com", "11223344", "pwd", "Camioneta");

        var fechaBase = DateTime.UtcNow;
        var pedido1 = new Pedido(1, fechaBase.AddHours(4), PrioridadPedido.Baja, "Pedido Normal");
        var pedido2 = new Pedido(1, fechaBase.AddHours(1), PrioridadPedido.Urgente, "Pedido Urgente 1");
        var pedido3 = new Pedido(1, fechaBase.AddHours(2), PrioridadPedido.Alta, "Pedido Ya Entregado");
        pedido3.CambiarEstado(EstadoPedido.Entregado);

        repartidor.AgregarPedido(pedido1);
        repartidor.AgregarPedido(pedido2);
        repartidor.AgregarPedido(pedido3);

        // a) Recorrido 1: Filtrar los que CUMPLEN (Entregados)
        var entregados = repartidor.ObtenerPedidosEntregados().ToList();
        Assert.Single(entregados);
        Assert.Contains(pedido3, entregados);

        // b) Recorrido 2: Filtrar los que NO CUMPLEN (No entregados)
        var noEntregados = repartidor.ObtenerPedidosNoEntregados().ToList();
        Assert.Equal(2, noEntregados.Count);
        Assert.DoesNotContain(pedido3, noEntregados);

        // c) Recorrido 3: Mapeo / Transformación
        var resumen = repartidor.ObtenerResumenPedidos().ToList();
        Assert.Equal(3, resumen.Count);
        Assert.All(resumen, item => Assert.StartsWith("Pedido #", item));

        // d) Recorrido 4: Encontrar primer elemento que cumple (Primer urgente no entregado)
        var primerUrgente = repartidor.BuscarPrimerPedidoUrgente();
        Assert.NotNull(primerUrgente);
        Assert.Equal("Pedido Urgente 1", primerUrgente!.Descripcion);

        // e) Estructura Clave-Valor (Dictionary con conteo por estado)
        var conteo = repartidor.ConteoPedidosPorEstado();
        Assert.Equal(2, conteo[EstadoPedido.Pendiente]);
        Assert.Equal(1, conteo[EstadoPedido.Entregado]);

        // f) Salida ordenada con criterio explícito (Fecha pactada ascendente: pedido2 (1h), pedido3 (2h), pedido1 (4h))
        var ordenados = repartidor.ObtenerPedidosOrdenadosPorFechaPactada().ToList();
        Assert.Equal(pedido2, ordenados[0]);
        Assert.Equal(pedido3, ordenados[1]);
        Assert.Equal(pedido1, ordenados[2]);
    }
}
