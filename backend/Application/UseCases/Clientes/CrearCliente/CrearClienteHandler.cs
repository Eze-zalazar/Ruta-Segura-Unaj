namespace Application.UseCases.Clientes.CrearCliente;

using Application.Interfaces.Persistence;
using Domain.Entities;

public class CrearClienteHandler
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CrearClienteHandler(
        IClienteRepository clienteRepository,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> HandleAsync(CrearClienteCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cliente = new Cliente(
            command.Nombre,
            command.Telefono,
            command.Direccion,
            command.Referencia);

        await _clienteRepository.AddAsync(cliente, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return cliente.Id;
    }
}
