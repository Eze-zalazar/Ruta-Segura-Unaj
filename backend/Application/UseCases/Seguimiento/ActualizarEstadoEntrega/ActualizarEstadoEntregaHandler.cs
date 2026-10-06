namespace Application.UseCases.Seguimiento.ActualizarEstadoEntrega;

using Application.Interfaces.Persistence;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

public class ActualizarEstadoEntregaHandler
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarEstadoEntregaHandler(IPedidoRepository pedidoRepository, IUnitOfWork unitOfWork)
    {
        _pedidoRepository = pedidoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(ActualizarEstadoEntregaCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pedido = await _pedidoRepository.GetByIdAsync(command.PedidoId, cancellationToken);
        if (pedido == null)
            throw new DomainException($"No se encontró ningún pedido con ID {command.PedidoId}.");

        // Si ya está en el estado solicitado, se maneja de forma idempotente sin error
        if (pedido.Estado == command.NuevoEstado)
            return;

        switch (command.NuevoEstado)
        {
            case EstadoPedido.EnCamino:
                pedido.MarcarEnCamino();
                break;
            case EstadoPedido.Entregado:
                pedido.RegistrarEntrega();
                break;
            case EstadoPedido.Cancelado:
                pedido.Cancelar();
                break;
            default:
                pedido.CambiarEstado(command.NuevoEstado);
                break;
        }

        _pedidoRepository.Update(pedido);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
