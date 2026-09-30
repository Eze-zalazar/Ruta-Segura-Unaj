namespace Application.UseCases.Clientes.ActualizarCliente;

using Application.Interfaces.Persistence;
using Domain.Exceptions;

public class ActualizarClienteHandler
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarClienteHandler(
        IClienteRepository clienteRepository,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(ActualizarClienteCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cliente = await _clienteRepository.GetByIdAsync(command.Id, cancellationToken);
        if (cliente == null)
            throw new DomainException($"No se encontró ningún cliente con el ID {command.Id}.");

        cliente.ActualizarNombre(command.Nombre);
        cliente.ActualizarContacto(command.Telefono, command.Direccion, command.Referencia);

        _clienteRepository.Update(cliente);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
