namespace Infrastructure.Repositories;

using Application.Interfaces.Persistence;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class RepartidorRepository : IRepartidorRepository
{
    private readonly AppDbContext _context;

    public RepartidorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Repartidor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Repartidores.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Repartidor>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Repartidores.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Repartidor>> GetDisponiblesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Repartidores
            .Where(r => r.Disponible)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Repartidor repartidor, CancellationToken cancellationToken = default)
    {
        await _context.Repartidores.AddAsync(repartidor, cancellationToken);
    }

    public void Update(Repartidor repartidor)
    {
        _context.Repartidores.Update(repartidor);
    }

    public void Delete(Repartidor repartidor)
    {
        _context.Repartidores.Remove(repartidor);
    }
}
