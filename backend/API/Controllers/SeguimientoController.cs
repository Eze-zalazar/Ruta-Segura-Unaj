namespace API.Controllers;

using Application.DTOs;
using Application.UseCases.Seguimiento.ActualizarEstadoEntrega;
using Application.UseCases.Seguimiento.ConsultarHistorialEntregas;
using Application.UseCases.Seguimiento.RegistrarIncidencia;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SeguimientoController : ControllerBase
{
    private readonly ActualizarEstadoEntregaHandler _actualizarEstadoHandler;
    private readonly RegistrarIncidenciaHandler _registrarIncidenciaHandler;
    private readonly ConsultarHistorialEntregasHandler _historialHandler;

    public SeguimientoController(
        ActualizarEstadoEntregaHandler actualizarEstadoHandler,
        RegistrarIncidenciaHandler registrarIncidenciaHandler,
        ConsultarHistorialEntregasHandler historialHandler)
    {
        _actualizarEstadoHandler = actualizarEstadoHandler;
        _registrarIncidenciaHandler = registrarIncidenciaHandler;
        _historialHandler = historialHandler;
    }

    // RF12: Actualizar estado de entrega (EnCamino, Entregado, Cancelado)
    [HttpPatch("{pedidoId:int}/estado")]
    public async Task<ActionResult> ActualizarEstado(int pedidoId, [FromBody] ActualizarEstadoRequest request, CancellationToken ct)
    {
        var command = new ActualizarEstadoEntregaCommand(pedidoId, request.NuevoEstado);
        await _actualizarEstadoHandler.HandleAsync(command, ct);
        return NoContent();
    }

    // RF13: Registrar incidencias sobre un pedido
    [HttpPost("{pedidoId:int}/incidencias")]
    public async Task<ActionResult<IncidenciaDto>> RegistrarIncidencia(int pedidoId, [FromBody] RegistrarIncidenciaRequest request, CancellationToken ct)
    {
        var command = new RegistrarIncidenciaCommand(pedidoId, request.Tipo, request.Descripcion);
        var incidencia = await _registrarIncidenciaHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetHistorial), new { pedidoId }, incidencia);
    }

    // RF14: Consulta cronológica de historial de entregas
    [HttpGet("historial")]
    public async Task<ActionResult<IReadOnlyList<HistorialEntregaDto>>> GetHistorial(
        [FromQuery] bool? soloEntregados,
        [FromQuery] bool? soloConIncidencias,
        [FromQuery] int? repartidorId,
        CancellationToken ct)
    {
        var query = new ConsultarHistorialEntregasQuery(soloEntregados, soloConIncidencias, repartidorId);
        var historial = await _historialHandler.HandleAsync(query, ct);
        return Ok(historial);
    }

    // RF14: Estructura clave-valor (Dictionary<string, int>) - Conteo de incidencias por tipo
    [HttpGet("conteo-incidencias")]
    public async Task<ActionResult<Dictionary<string, int>>> GetConteoIncidencias(CancellationToken ct)
    {
        var conteos = await _historialHandler.ObtenerConteoIncidenciasPorTipoAsync(ct);
        return Ok(conteos);
    }

    // RF14: Estructura clave-valor (Dictionary<string, int>) - Entregas por repartidor
    [HttpGet("entregas-por-repartidor")]
    public async Task<ActionResult<Dictionary<string, int>>> GetEntregasPorRepartidor(CancellationToken ct)
    {
        var entregas = await _historialHandler.ObtenerEntregasConcretadasPorRepartidorAsync(ct);
        return Ok(entregas);
    }
}

public record ActualizarEstadoRequest(EstadoPedido NuevoEstado);
public record RegistrarIncidenciaRequest(string Tipo, string Descripcion);
