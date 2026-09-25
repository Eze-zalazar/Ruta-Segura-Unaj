namespace Application.UseCases.Repartidores.ObtenerRepartidores;

using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Mappings;

public class ObtenerRepartidoresHandler
{
    private readonly IRepartidorRepository _repartidorRepository;

    public ObtenerRepartidoresHandler(IRepartidorRepository repartidorRepository)
    {
        _repartidorRepository = repartidorRepository;
    }

    public async Task<IReadOnlyList<RepartidorDto>> HandleAsync(ObtenerRepartidoresQuery query, CancellationToken cancellationToken = default)
    {
        var repartidores = query.SoloDisponibles == true
            ? await _repartidorRepository.GetDisponiblesAsync(cancellationToken)
            : await _repartidorRepository.GetAllAsync(cancellationToken);

        return repartidores.Select(r => r.ToDto()).ToList();
    }
}
