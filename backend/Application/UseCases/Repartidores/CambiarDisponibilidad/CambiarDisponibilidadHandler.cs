namespace Application.UseCases.Repartidores.CambiarDisponibilidad;

using Application.Interfaces.Persistence;
using Domain.Exceptions;

public class CambiarDisponibilidadHandler
{
    private readonly IRepartidorRepository _repartidorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CambiarDisponibilidadHandler(IRepartidorRepository repartidorRepository, IUnitOfWork unitOfWork)
    {
        _repartidorRepository = repartidorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(CambiarDisponibilidadCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var repartidor = await _repartidorRepository.GetByIdAsync(command.Id, cancellationToken);
        if (repartidor == null)
            throw new DomainException($"No se encontró ningún repartidor con ID {command.Id}.");

        repartidor.SetDisponible(command.Disponible);

        _repartidorRepository.Update(repartidor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
