namespace Application.Interfaces.Persistence;

using Domain.Entities;
using Domain.Enums;

public interface IPedidoRepository
{
    Task<Pedido?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Pedido>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Pedido>> FiltrarAsync(EstadoPedido? estado, DateTime? fecha, int? clienteId, CancellationToken cancellationToken = default);
    Task AddAsync(Pedido pedido, CancellationToken cancellationToken = default);
    void Update(Pedido pedido);
}
