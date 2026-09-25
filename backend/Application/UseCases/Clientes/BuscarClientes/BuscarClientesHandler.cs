namespace Application.UseCases.Clientes.BuscarClientes;

using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Mappings;

public class BuscarClientesHandler
{
    private readonly IClienteRepository _clienteRepository;

    public BuscarClientesHandler(IClienteRepository clienteRepository)
    {
        _clienteRepository = clienteRepository;
    }

    public async Task<IReadOnlyList<ClienteDto>> HandleAsync(BuscarClientesQuery query, CancellationToken cancellationToken = default)
    {
        var clientes = await _clienteRepository.BuscarAsync(query.Termino, cancellationToken);
        return clientes.Select(c => c.ToDto()).ToList();
    }
}
