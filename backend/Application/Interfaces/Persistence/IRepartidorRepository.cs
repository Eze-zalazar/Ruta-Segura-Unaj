namespace Application.Interfaces.Persistence;

using Domain.Entities;

public interface IRepartidorRepository
{
    Task<Repartidor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Repartidor>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Repartidor>> GetDisponiblesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Repartidor repartidor, CancellationToken cancellationToken = default);
    void Update(Repartidor repartidor);
    void Delete(Repartidor repartidor);
}
