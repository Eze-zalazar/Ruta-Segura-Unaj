namespace Application.UseCases.Pedidos.CrearPedido;

using Application.Interfaces.Persistence;
using Domain.Entities;
using Domain.Exceptions;

public class CrearPedidoHandler
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CrearPedidoHandler(
        IPedidoRepository pedidoRepository,
        IClienteRepository clienteRepository,
        IUnitOfWork unitOfWork)
    {
        _pedidoRepository = pedidoRepository;
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> HandleAsync(CrearPedidoCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ClienteId <= 0)
            throw new DomainException("El pedido debe estar asociado a un cliente válido.");

        var cliente = await _clienteRepository.GetByIdAsync(command.ClienteId, cancellationToken);
        if (cliente == null)
            throw new DomainException($"No existe ningún cliente registrado con el ID {command.ClienteId}.");

        var pedido = new Pedido(
            command.ClienteId,
            command.FechaPactada,
            command.Prioridad,
            command.Descripcion,
            command.Observaciones);

        await _pedidoRepository.AddAsync(pedido, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return pedido.Id;
    }
}
