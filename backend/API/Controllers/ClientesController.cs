namespace API.Controllers;

using Application.DTOs;
using Application.UseCases.Clientes.BuscarClientes;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly BuscarClientesHandler _buscarHandler;

    public ClientesController(BuscarClientesHandler buscarHandler)
    {
        _buscarHandler = buscarHandler;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClienteDto>>> Buscar([FromQuery] string? termino, CancellationToken ct)
    {
        var clientes = await _buscarHandler.HandleAsync(new BuscarClientesQuery(termino), ct);
        return Ok(clientes);
    }
}
