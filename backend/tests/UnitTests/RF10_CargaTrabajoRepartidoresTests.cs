namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Repartidores.ConsultarCargaTrabajo;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Xunit;

public class RF10_CargaTrabajoRepartidoresTests
{
    private readonly Mock<IRepartidorRepository> _repartidorRepo = new();

    // 1. Camino Feliz: Cálculo de carga activa y estructura clave-valor (Dictionary<string, int>)
    [Fact]
    public async Task ConsultarCargaTrabajo_CalculaCargaYGeneraDiccionarioClaveValor()
    {
        var r1 = new Repartidor(1, "Ana", "ana@correo.com", "111", "pwd", "Moto");
        var r2 = new Repartidor(2, "Beto", "beto@correo.com", "222", "pwd", "Auto");

        var p1 = new Pedido(1, 10, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Pedido 1");
        var p2 = new Pedido(2, 10, DateTime.UtcNow.AddDays(1), PrioridadPedido.Alta, "Pedido 2");
        r1.AsignarPedido(p1);
        r1.AsignarPedido(p2);

        _repartidorRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<Repartidor> { r1, r2 });

        var handler = new ConsultarCargaTrabajoHandler(_repartidorRepo.Object);

        // Estructura clave-valor (Dictionary<string, int>)
        var diccionario = await handler.ObtenerConteoAsignacionesPorRepartidorAsync();
        Assert.Equal(2, diccionario["Ana"]);
        Assert.Equal(0, diccionario["Beto"]);

        // Lista ordenada por carga de trabajo (Beto con 0 pedidos va primero que Ana con 2)
        var lista = await handler.HandleAsync(new ConsultarCargaTrabajoQuery());
        Assert.Equal("Beto", lista[0].Nombre);
        Assert.Equal(0, lista[0].CargaActiva);
        Assert.Equal("Ana", lista[1].Nombre);
        Assert.Equal(2, lista[1].CargaActiva);
    }

    // 2. Caso Borde: Repartidor sin pedidos (carga 0 / colección vacía) y primer disponible (FirstOrDefault)
    [Fact]
    public async Task ConsultarCargaTrabajo_SinPedidosYPrimerDisponible_RetornaColeccionVaciaYPrimerElemento()
    {
        var r1 = new Repartidor(1, "Dario", "dario@correo.com", "333", "pwd", "Bici");
        _repartidorRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<Repartidor> { r1 });

        var handler = new ConsultarCargaTrabajoHandler(_repartidorRepo.Object);
        var primer = await handler.ObtenerPrimerDisponibleMenorCargaAsync();

        Assert.NotNull(primer);
        Assert.Equal("Dario", primer.Nombre);
        Assert.Equal(0, primer.CargaActiva);
        Assert.True(primer.TieneCapacidad);
    }

    // 3. Caso Filtrado / Exclusión: Exclusión de repartidores sin capacidad o no disponibles (Where negado)
    [Fact]
    public async Task ConsultarCargaTrabajo_FiltroCapacidad_ExcluyeNoDisponiblesOCargaCompleta()
    {
        var rDisponible = new Repartidor(1, "Elena", "elena@correo.com", "444", "pwd", "Moto", disponible: true, capacidadMaxima: 2);
        var rNoDisponible = new Repartidor(2, "Franco", "franco@correo.com", "555", "pwd", "Auto", disponible: false);
        var rLleno = new Repartidor(3, "Gisela", "gisela@correo.com", "666", "pwd", "Furgoneta", disponible: true, capacidadMaxima: 1);

        rLleno.AsignarPedido(new Pedido(1, 10, DateTime.UtcNow.AddDays(1), PrioridadPedido.Baja, "P1"));

        _repartidorRepo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<Repartidor> { rDisponible, rNoDisponible, rLleno });

        var handler = new ConsultarCargaTrabajoHandler(_repartidorRepo.Object);
        var soloConCapacidad = await handler.HandleAsync(new ConsultarCargaTrabajoQuery(SoloConCapacidad: true));

        Assert.Single(soloConCapacidad);
        Assert.Equal("Elena", soloConCapacidad[0].Nombre);
    }
}
