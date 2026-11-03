namespace Infrastructure.Repositories;

using Application.Interfaces.Persistence;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Cliente>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Clientes.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cliente>> BuscarAsync(string? termino, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(termino))
            return await GetAllAsync(cancellationToken);

        termino = termino.Trim().ToLower();
        return await _context.Clientes
            .Where(c => c.Nombre.ToLower().Contains(termino) ||
                        c.Direccion.ToLower().Contains(termino) ||
                        c.Telefono.Contains(termino))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        await _context.Clientes.AddAsync(cliente, cancellationToken);
    }

    public void Update(Cliente cliente)
    {
        _context.Clientes.Update(cliente);
    }

    public void Delete(Cliente cliente)
    {
        _context.Clientes.Remove(cliente);
    }
}
