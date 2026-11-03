namespace Application.Interfaces.Persistence;

using Domain.Entities;

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cliente>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cliente>> BuscarAsync(string? termino, CancellationToken cancellationToken = default);
    Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default);
    void Update(Cliente cliente);
    void Delete(Cliente cliente);
}
