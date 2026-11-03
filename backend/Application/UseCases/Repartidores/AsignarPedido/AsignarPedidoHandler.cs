
namespace Application.UseCases.Repartidores.AsignarPedido;

using Application.Interfaces.Persistence;
using Domain.Exceptions;

public class AsignarPedidoHandler
{
    private readonly IRepartidorRepository _repartidorRepository;
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AsignarPedidoHandler(
        IRepartidorRepository repartidorRepository,
        IPedidoRepository pedidoRepository,
        IUnitOfWork unitOfWork)
    {
        _repartidorRepository = repartidorRepository;
        _pedidoRepository = pedidoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        AsignarPedidoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var repartidor = await _repartidorRepository.GetByIdAsync(
            command.RepartidorId,
            cancellationToken);

        if (repartidor is null)
        {
            throw new DomainException(
                $"No se encontró ningún repartidor con ID {command.RepartidorId}.");
        }

        var pedido = await _pedidoRepository.GetByIdAsync(
            command.PedidoId,
            cancellationToken);

        if (pedido is null)
        {
            throw new DomainException(
                $"No se encontró ningún pedido con ID {command.PedidoId}.");
        }

        repartidor.AsignarPedido(pedido);

        _repartidorRepository.Update(repartidor);
        _pedidoRepository.Update(pedido);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}