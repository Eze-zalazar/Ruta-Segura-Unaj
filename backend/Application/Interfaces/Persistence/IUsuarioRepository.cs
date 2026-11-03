namespace Application.Interfaces.Persistence;

using Domain.Entities;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default);
    void Update(Usuario usuario);
    void Delete(Usuario usuario);
}
