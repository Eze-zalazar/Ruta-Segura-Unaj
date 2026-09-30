namespace UnitTests;

using Application.Interfaces.Persistence;
using Application.UseCases.Clientes.ActualizarCliente;
using Application.UseCases.Clientes.EliminarCliente;
using Application.UseCases.Clientes.ObtenerClientePorId;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Moq;
using Xunit;

/// <summary>
/// RF04: Edición y baja de clientes.
/// Regla de 3 pruebas: Camino Normal, Caso de Borde y Caso de Error (Regla de negocio).
/// </summary>
public class RF04_EdicionBajaClientesTests
{
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();

    // a) Camino Normal (Happy Path: Actualización y baja sin pedidos)
    [Fact]
    public async Task ActualizarYEliminarCliente_SinPedidos_DebeModificarYDarDeBajaCorrectamente()
    {
        // Arrange 1: Actualizar
        var cliente = new Cliente("Panadería Centro", "11223344", "Pringles 120");
        _clienteRepoMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        var actHandler = new ActualizarClienteHandler(_clienteRepoMock.Object, _uowMock.Object);
        var actCmd = new ActualizarClienteCommand(5, "Panadería Centro SRL", "11999988", "Pringles 150", "Local 2");

        await actHandler.HandleAsync(actCmd);

        Assert.Equal("Panadería Centro SRL", cliente.Nombre);
        Assert.Equal("11999988", cliente.Telefono);
        Assert.Equal("Pringles 150", cliente.Direccion);
        _clienteRepoMock.Verify(r => r.Update(cliente), Times.Once);

        // Arrange 2: Baja de cliente sin pedidos
        var elimHandler = new EliminarClienteHandler(_clienteRepoMock.Object, _uowMock.Object);
        await elimHandler.HandleAsync(new EliminarClienteCommand(5));

        _clienteRepoMock.Verify(r => r.Delete(cliente), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    // b) Caso de Borde (Consulta por ID inexistente maneja ausencia sin excepción)
    [Fact]
    public async Task ObtenerClientePorId_CuandoNoExiste_DebeRetornarNullOManejarAusencia()
    {
        // Arrange
        _clienteRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente?)null);

        var handler = new ObtenerClientePorIdHandler(_clienteRepoMock.Object);

        // Act
        var result = await handler.HandleAsync(new ObtenerClientePorIdQuery(999));

        // Assert
        Assert.Null(result);
    }

    // c) Caso de Error / Regla de Negocio (Prohibición de baja con pedidos activos)
    [Fact]
    public async Task EliminarCliente_ConPedidosAsociados_DebeLanzarDomainExceptionPorReglaDeNegocio()
    {
        // Arrange: Cliente con pedidos cargados en el sistema
        var clienteConPedidos = new Cliente("Kiosco Moreno", "11334455", "Moreno 300");
        var pedidoActivo = new Pedido(1, DateTime.UtcNow.AddHours(3), PrioridadPedido.Alta, "Gaseosas y golosinas");
        clienteConPedidos.AgregarPedido(pedidoActivo);

        _clienteRepoMock
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clienteConPedidos);

        var handler = new EliminarClienteHandler(_clienteRepoMock.Object, _uowMock.Object);

        // Act & Assert: La regla de integridad prohíbe dar de baja el cliente
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(new EliminarClienteCommand(2)));
        Assert.Contains("No se puede eliminar un cliente que tiene pedidos asociados", ex.Message);
        _clienteRepoMock.Verify(r => r.Delete(It.IsAny<Cliente>()), Times.Never);
    }
}
