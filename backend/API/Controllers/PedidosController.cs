namespace API.Controllers;

using Application.DTOs;
using Application.UseCases.Pedidos.ActualizarPedido;
using Application.UseCases.Pedidos.CancelarPedido;
using Application.UseCases.Pedidos.CrearPedido;
using Application.UseCases.Pedidos.FiltrarPedidos;
using Application.UseCases.Pedidos.ObtenerPedidoPorId;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly CrearPedidoHandler _crearHandler;
    private readonly ActualizarPedidoHandler _actualizarHandler;
    private readonly CancelarPedidoHandler _cancelarHandler;
    private readonly FiltrarPedidosHandler _filtrarHandler;
    private readonly ObtenerPedidoPorIdHandler _obtenerPorIdHandler;

    public PedidosController(
        CrearPedidoHandler crearHandler,
        ActualizarPedidoHandler actualizarHandler,
        CancelarPedidoHandler cancelarHandler,
        FiltrarPedidosHandler filtrarHandler,
        ObtenerPedidoPorIdHandler obtenerPorIdHandler)
    {
        _crearHandler = crearHandler;
        _actualizarHandler = actualizarHandler;
        _cancelarHandler = cancelarHandler;
        _filtrarHandler = filtrarHandler;
        _obtenerPorIdHandler = obtenerPorIdHandler;
    }

    // RF08: Listado y filtro de pedidos (ordenados por fecha pactada y mapeo a DTO)
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PedidoDto>>> Filtrar(
        [FromQuery] EstadoPedido? estado,
        [FromQuery] DateTime? fecha,
        [FromQuery] int? clienteId,
        CancellationToken ct)
    {
        var pedidos = await _filtrarHandler.HandleAsync(new FiltrarPedidosQuery(estado, fecha, clienteId), ct);
        return Ok(pedidos);
    }

    // Consulta individual para detalle
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoDto>> GetById(int id, CancellationToken ct)
    {
        var pedido = await _obtenerPorIdHandler.HandleAsync(new ObtenerPedidoPorIdQuery(id), ct);
        if (pedido == null)
            return NotFound(new { mensaje = $"No se encontró ningún pedido con el ID {id}." });

        return Ok(pedido);
    }

    // RF06: Alta de pedidos
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CrearPedidoCommand command, CancellationToken ct)
    {
        var id = await _crearHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    // RF07: Edición de campos permitidos
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] ActualizarPedidoRequest request, CancellationToken ct)
    {
        var command = new ActualizarPedidoCommand(id, request.Descripcion, request.FechaPactada, request.Prioridad, request.Observaciones);
        await _actualizarHandler.HandleAsync(command, ct);
        return NoContent();
    }

    // RF07: Cancelación de pedidos
    [HttpPost("{id:int}/cancelar")]
    [HttpPatch("{id:int}/cancelar")]
    public async Task<ActionResult> Cancelar(int id, CancellationToken ct)
    {
        var command = new CancelarPedidoCommand(id);
        await _cancelarHandler.HandleAsync(command, ct);
        return NoContent();
    }
}

public record ActualizarPedidoRequest(
    string Descripcion,
    DateTime FechaPactada,
    PrioridadPedido Prioridad,
    string? Observaciones = null);
