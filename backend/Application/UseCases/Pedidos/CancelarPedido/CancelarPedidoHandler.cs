
namespace Application.UseCases.Pedidos.CancelarPedido;

using Application.Interfaces.Persistence;
using Domain.Exceptions;

public class CancelarPedidoHandler
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarPedidoHandler(
        IPedidoRepository pedidoRepository,
        IUnitOfWork unitOfWork)
    {
        _pedidoRepository = pedidoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        CancelarPedidoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pedido = await _pedidoRepository.GetByIdAsync(
            command.Id,
            cancellationToken);

        if (pedido is null)
        {
            throw new DomainException(
                $"No se encontró ningún pedido con el ID {command.Id}.");
        }

        pedido.Cancelar();

        _pedidoRepository.Update(pedido);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

