namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Repartidores.ActualizarRepartidor;
using Application.UseCases.Repartidores.CambiarDisponibilidad;
using Domain.Entities;
using Domain.Exceptions;
using Moq;
using Xunit;

public class RF09_ABMRepartidoresTests
{
    private readonly Mock<IRepartidorRepository> _repartidorRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // 1. Camino Feliz: Actualización exitosa de datos y vehículo
    [Fact]
    public async Task ActualizarRepartidor_DatosValidos_ModificaYPersisteCorrectamente()
    {
        var repartidor = new Repartidor("Carlos", "carlos@correo.com", "112233", "pwd", "Moto");
        _repartidorRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(repartidor);

        var handler = new ActualizarRepartidorHandler(_repartidorRepo.Object, _uow.Object);
        await handler.HandleAsync(new ActualizarRepartidorCommand(1, "Carlos Actualizado", "119988", "Furgoneta"));

        Assert.Equal("Carlos Actualizado", repartidor.Nombre);
        Assert.Equal("119988", repartidor.Telefono);
        Assert.Equal("Furgoneta", repartidor.Vehiculo);
        _repartidorRepo.Verify(r => r.Update(repartidor), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 2. Caso Borde: Cambio de disponibilidad y espacios normalizados en vehículo
    [Fact]
    public async Task CambiarDisponibilidadYVehiculo_NormalizaYActualizaEstado()
    {
        var repartidor = new Repartidor("Martin", "martin@correo.com", "114455", "pwd", "  Bicicleta  ", disponible: true);
        _repartidorRepo.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync(repartidor);

        var dispHandler = new CambiarDisponibilidadHandler(_repartidorRepo.Object, _uow.Object);
        await dispHandler.HandleAsync(new CambiarDisponibilidadCommand(2, false));

        Assert.False(repartidor.Disponible);
        Assert.Equal("Bicicleta", repartidor.Vehiculo);
        _repartidorRepo.Verify(r => r.Update(repartidor), Times.Once);
    }

    // 3. Caso Error: Vehículo vacío o repartidor inexistente
    [Fact]
    public async Task ActualizarRepartidor_VehiculoVacioOInexistente_LanzaDomainException()
    {
        var repartidor = new Repartidor("Laura", "laura@correo.com", "115566", "pwd", "Moto");
        _repartidorRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(repartidor);
        _repartidorRepo.Setup(r => r.GetByIdAsync(999, default)).ReturnsAsync((Repartidor?)null);

        var handler = new ActualizarRepartidorHandler(_repartidorRepo.Object, _uow.Object);

        // Vehículo vacío
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new ActualizarRepartidorCommand(1, "Laura", "115566", "   ")));

        // Repartidor inexistente
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new ActualizarRepartidorCommand(999, "Inexistente", "1111", "Auto")));
    }
}
