namespace Application.UseCases.Repartidores.ObtenerRepartidorPorId;

using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Mappings;

public class ObtenerRepartidorPorIdHandler
{
    private readonly IRepartidorRepository _repartidorRepository;

    public ObtenerRepartidorPorIdHandler(IRepartidorRepository repartidorRepository)
    {
        _repartidorRepository = repartidorRepository;
    }

    public async Task<RepartidorDto?> HandleAsync(ObtenerRepartidorPorIdQuery query, CancellationToken cancellationToken = default)
    {
        var repartidor = await _repartidorRepository.GetByIdAsync(query.Id, cancellationToken);
        return repartidor?.ToDto();
    }
}
