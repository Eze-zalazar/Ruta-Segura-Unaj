namespace Application.UseCases.Repartidores.ActualizarRepartidor;

using Application.Interfaces.Persistence;
using Domain.Exceptions;

public class ActualizarRepartidorHandler
{
    private readonly IRepartidorRepository _repartidorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarRepartidorHandler(IRepartidorRepository repartidorRepository, IUnitOfWork unitOfWork)
    {
        _repartidorRepository = repartidorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(ActualizarRepartidorCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var repartidor = await _repartidorRepository.GetByIdAsync(command.Id, cancellationToken);
        if (repartidor == null)
            throw new DomainException($"No se encontró ningún repartidor con ID {command.Id}.");

        repartidor.ActualizarDatosBasicos(command.Nombre, command.Telefono);
        repartidor.ActualizarVehiculo(command.Vehiculo);

        _repartidorRepository.Update(repartidor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
