namespace Application.UseCases.Pedidos.ActualizarPedido;

using Application.Interfaces.Persistence;
using Domain.Exceptions;

public class ActualizarPedidoHandler
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarPedidoHandler(
        IPedidoRepository pedidoRepository,
        IUnitOfWork unitOfWork)
    {
        _pedidoRepository = pedidoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(ActualizarPedidoCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pedido = await _pedidoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (pedido == null)
            throw new DomainException($"No se encontró ningún pedido con el ID {command.Id}.");

        pedido.ActualizarDatos(command.Descripcion, command.FechaPactada, command.Prioridad, command.Observaciones);

        _pedidoRepository.Update(pedido);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
