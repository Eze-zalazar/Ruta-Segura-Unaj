namespace Infrastructure.Repositories;

using Application.Interfaces.Persistence;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class PedidoRepository : IPedidoRepository
{
    private readonly AppDbContext _context;

    public PedidoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Pedido?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Repartidor)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Pedido>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Repartidor)
            .OrderBy(p => p.FechaPactada)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Pedido>> FiltrarAsync(EstadoPedido? estado, DateTime? fecha, int? clienteId, CancellationToken cancellationToken = default)
    {
        IQueryable<Pedido> query = _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Repartidor);

        if (estado.HasValue)
        {
            query = query.Where(p => p.Estado == estado.Value);
        }

        if (fecha.HasValue)
        {
            var fechaInicio = fecha.Value.Date;
            var fechaFin = fechaInicio.AddDays(1);
            query = query.Where(p => p.FechaPactada >= fechaInicio && p.FechaPactada < fechaFin);
        }

        if (clienteId.HasValue)
        {
            query = query.Where(p => p.ClienteId == clienteId.Value);
        }

        return await query
            .OrderBy(p => p.FechaPactada)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        await _context.Pedidos.AddAsync(pedido, cancellationToken);
    }

    public void Update(Pedido pedido)
    {
        _context.Pedidos.Update(pedido);
    }
}
