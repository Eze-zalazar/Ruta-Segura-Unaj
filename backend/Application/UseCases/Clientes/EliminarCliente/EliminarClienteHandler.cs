namespace Application.UseCases.Clientes.EliminarCliente;

using Application.Interfaces.Persistence;
using Domain.Exceptions;

public class EliminarClienteHandler
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EliminarClienteHandler(
        IClienteRepository clienteRepository,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(EliminarClienteCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cliente = await _clienteRepository.GetByIdAsync(command.Id, cancellationToken);
        if (cliente == null)
            throw new DomainException($"No se encontró ningún cliente con el ID {command.Id}.");

        if (cliente.Pedidos.Count > 0)
            throw new DomainException("No se puede eliminar un cliente que tiene pedidos asociados.");

        _clienteRepository.Delete(cliente);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
