namespace Application.UseCases.Clientes.ObtenerClientePorId;

using Application.DTOs;
using Application.Interfaces.Persistence;
using Application.Mappings;

public class ObtenerClientePorIdHandler
{
    private readonly IClienteRepository _clienteRepository;

    public ObtenerClientePorIdHandler(IClienteRepository clienteRepository)
    {
        _clienteRepository = clienteRepository;
    }

    public async Task<ClienteDto?> HandleAsync(ObtenerClientePorIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cliente = await _clienteRepository.GetByIdAsync(query.Id, cancellationToken);
        return cliente?.ToDto();
    }
}
