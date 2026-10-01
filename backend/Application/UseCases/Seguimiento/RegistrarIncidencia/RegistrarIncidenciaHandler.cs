namespace Application.UseCases.Seguimiento.RegistrarIncidencia;

using Application.DTOs;
using Application.Interfaces.Persistence;
using Domain.Entities;
using Domain.Exceptions;

public class RegistrarIncidenciaHandler
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarIncidenciaHandler(IPedidoRepository pedidoRepository, IUnitOfWork unitOfWork)
    {
        _pedidoRepository = pedidoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IncidenciaDto> HandleAsync(RegistrarIncidenciaCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pedido = await _pedidoRepository.GetByIdAsync(command.PedidoId, cancellationToken);
        if (pedido == null)
            throw new DomainException($"No se encontró ningún pedido con ID {command.PedidoId}.");

        var incidencia = new Incidencia(command.PedidoId, command.Tipo, command.Descripcion);
        pedido.RegistrarIncidencia(incidencia);

        _pedidoRepository.Update(pedido);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new IncidenciaDto(
            incidencia.Id,
            incidencia.PedidoId,
            incidencia.Tipo,
            incidencia.Descripcion,
            incidencia.FechaHora,
            incidencia.Resuelta);
    }
}
