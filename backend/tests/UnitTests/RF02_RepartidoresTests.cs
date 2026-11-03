namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Repartidores.CrearRepartidor;
using Domain.Entities;
using Domain.Exceptions;
using Moq;
using Xunit;

public class RF02_RepartidoresTests
{
    private readonly Mock<IRepartidorRepository> _repartidorRepo = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // 1. Camino Feliz: Alta de repartidor
    [Fact]
    public async Task CrearRepartidor_DatosValidos_PersisteYRetornaId()
    {
        _usuarioRepo.Setup(r => r.GetByEmailAsync("juan@correo.com", default)).ReturnsAsync((Usuario?)null);
        var handler = new CrearRepartidorHandler(_repartidorRepo.Object, _usuarioRepo.Object, _uow.Object);

        await handler.HandleAsync(new CrearRepartidorCommand("Juan", "juan@correo.com", "112233", "123456", "Moto"));

        _repartidorRepo.Verify(r => r.AddAsync(It.IsAny<Repartidor>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 2. Caso Borde: Repartidor no disponible bloquea asignación de pedidos
    [Fact]
    public void Repartidor_NoDisponible_NoPuedeRecibirEntregas()
    {
        var repartidor = new Repartidor("Juan", "juan@correo.com", "112233", "pwd", "Moto", disponible: false);

        Assert.False(repartidor.Disponible);
        Assert.False(repartidor.PuedeAsignarseEntregas());
    }

    // 3. Caso Error: Email duplicado
    [Fact]
    public async Task CrearRepartidor_EmailDuplicado_LanzaDomainException()
    {
        var existente = new Repartidor("Otro", "juan@correo.com", "1111", "pwd", "Auto");
        _usuarioRepo.Setup(r => r.GetByEmailAsync("juan@correo.com", default)).ReturnsAsync(existente);

        var handler = new CrearRepartidorHandler(_repartidorRepo.Object, _usuarioRepo.Object, _uow.Object);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CrearRepartidorCommand("Juan", "juan@correo.com", "112233", "123456", "Moto")));
        Assert.Contains("Ya existe un usuario", ex.Message);
    }
}
