namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Clientes.ActualizarCliente;
using Application.UseCases.Clientes.EliminarCliente;
using Application.UseCases.Clientes.ObtenerClientePorId;
using Domain.Entities;
using Domain.Exceptions;
using Moq;
using Xunit;

public class RF04_EdicionBajaClientesTests
{
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // 1. Camino Feliz: Modificación exitosa
    [Fact]
    public async Task ActualizarCliente_DatosValidos_ModificaYPersiste()
    {
        var cliente = new Cliente("Original", "1111", "Dir 1");
        _clienteRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(cliente);

        var handler = new ActualizarClienteHandler(_clienteRepo.Object, _uow.Object);
        await handler.HandleAsync(new ActualizarClienteCommand(1, "Actualizado", "2222", "Dir 2"));

        Assert.Equal("Actualizado", cliente.Nombre);
        Assert.Equal("2222", cliente.Telefono);
        _clienteRepo.Verify(r => r.Update(cliente), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 2. Caso Baja: Eliminación exitosa de cliente sin pedidos
    [Fact]
    public async Task EliminarCliente_SinPedidos_EliminaCorrectamente()
    {
        var cliente = new Cliente("Para Borrar", "1111", "Dir");
        _clienteRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(cliente);

        var handler = new EliminarClienteHandler(_clienteRepo.Object, _uow.Object);
        await handler.HandleAsync(new EliminarClienteCommand(1));

        _clienteRepo.Verify(r => r.Delete(cliente), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // 3. Caso Error / Ausencia: Cliente no encontrado
    [Fact]
    public async Task ObtenerCliente_IdInexistente_RetornaNull()
    {
        _clienteRepo.Setup(r => r.GetByIdAsync(999, default)).ReturnsAsync((Cliente?)null);
        var handler = new ObtenerClientePorIdHandler(_clienteRepo.Object);

        var result = await handler.HandleAsync(new ObtenerClientePorIdQuery(999));

        Assert.Null(result);
    }
}
