namespace API.Controllers;

using Application.DTOs;
using Application.UseCases.Clientes.ActualizarCliente;
using Application.UseCases.Clientes.BuscarClientes;
using Application.UseCases.Clientes.CrearCliente;
using Application.UseCases.Clientes.EliminarCliente;
using Application.UseCases.Clientes.ObtenerClientePorId;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly BuscarClientesHandler _buscarHandler;
    private readonly ObtenerClientePorIdHandler _obtenerPorIdHandler;
    private readonly CrearClienteHandler _crearHandler;
    private readonly ActualizarClienteHandler _actualizarHandler;
    private readonly EliminarClienteHandler _eliminarHandler;

    public ClientesController(
        BuscarClientesHandler buscarHandler,
        ObtenerClientePorIdHandler obtenerPorIdHandler,
        CrearClienteHandler crearHandler,
        ActualizarClienteHandler actualizarHandler,
        EliminarClienteHandler eliminarHandler)
    {
        _buscarHandler = buscarHandler;
        _obtenerPorIdHandler = obtenerPorIdHandler;
        _crearHandler = crearHandler;
        _actualizarHandler = actualizarHandler;
        _eliminarHandler = eliminarHandler;
    }

    // RF05: Búsqueda y filtro de clientes (mantenido intacto)
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClienteDto>>> Buscar([FromQuery] string? termino, CancellationToken ct)
    {
        var clientes = await _buscarHandler.HandleAsync(new BuscarClientesQuery(termino), ct);
        return Ok(clientes);
    }

    // RF04: Consulta individual para edición/detalle
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> GetById(int id, CancellationToken ct)
    {
        var cliente = await _obtenerPorIdHandler.HandleAsync(new ObtenerClientePorIdQuery(id), ct);
        if (cliente == null)
            return NotFound(new { mensaje = $"No se encontró ningún cliente con el ID {id}." });

        return Ok(cliente);
    }

    // RF03: Alta de clientes
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CrearClienteCommand command, CancellationToken ct)
    {
        var id = await _crearHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    // RF04: Edición de clientes
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] ActualizarClienteRequest request, CancellationToken ct)
    {
        var command = new ActualizarClienteCommand(id, request.Nombre, request.Telefono, request.Direccion, request.Referencia);
        await _actualizarHandler.HandleAsync(command, ct);
        return NoContent();
    }

    // RF04: Baja de clientes
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var command = new EliminarClienteCommand(id);
        await _eliminarHandler.HandleAsync(command, ct);
        return NoContent();
    }
}

public record ActualizarClienteRequest(string Nombre, string Telefono, string Direccion, string? Referencia = null);
