namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Clientes.CrearCliente;
using Domain.Entities;
using Domain.Exceptions;
using Moq;
using Xunit;

public class RF03_AltaClientesTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // 1. Camino Feliz: Alta exitosa
    [Fact]
    public async Task CrearCliente_DatosValidos_PersisteYRetornaId()
    {
        var handler = new CrearClienteHandler(_clienteRepo.Object, _uow.Object);
        var cmd = new CrearClienteCommand("Kiosco Sol", "112233", "Av. San Martín 100", "Local 1");

        await handler.HandleAsync(cmd);

        _clienteRepo.Verify(r => r.AddAsync(It.IsAny<Cliente>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 2. Caso Error: Datos incompletos o vacíos
    [Fact]
    public async Task CrearCliente_DatosIncompletosOVacios_LanzaDomainException()
    {
        var handler = new CrearClienteHandler(_clienteRepo.Object, _uow.Object);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CrearClienteCommand("", "112233", "Direccion")));
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new CrearClienteCommand("Cliente", "", "Direccion")));
    }

    // 3. Caso Borde: Normalización de espacios y referencias opcionales vacías
    [Fact]
    public async Task CrearCliente_EspaciosOReferenciaVacia_NormalizaCorrectamente()
    {
        Cliente? creado = null;
        _clienteRepo.Setup(r => r.AddAsync(It.IsAny<Cliente>(), default))
            .Callback<Cliente, CancellationToken>((c, _) => creado = c)
            .Returns(Task.CompletedTask);

        var handler = new CrearClienteHandler(_clienteRepo.Object, _uow.Object);
        await handler.HandleAsync(new CrearClienteCommand("  Libreria Mitre  ", "  119988  ", "  Mitre 200  ", "   "));

        Assert.NotNull(creado);
        Assert.Equal("Libreria Mitre", creado.Nombre);
        Assert.Equal("119988", creado.Telefono);
        Assert.Null(creado.Referencia);
    }
}
