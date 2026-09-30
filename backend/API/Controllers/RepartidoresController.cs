namespace API.Controllers;

using Application.DTOs;
using Application.UseCases.Repartidores.ActualizarRepartidor;
using Application.UseCases.Repartidores.AsignarPedido;
using Application.UseCases.Repartidores.CambiarDisponibilidad;
using Application.UseCases.Repartidores.ConsultarCargaTrabajo;
using Application.UseCases.Repartidores.CrearRepartidor;
using Application.UseCases.Repartidores.ObtenerRepartidores;
using Application.UseCases.Repartidores.ObtenerRepartidorPorId;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RepartidoresController : ControllerBase
{
    private readonly CrearRepartidorHandler _crearHandler;
    private readonly ActualizarRepartidorHandler _actualizarHandler;
    private readonly CambiarDisponibilidadHandler _disponibilidadHandler;
    private readonly ObtenerRepartidoresHandler _obtenerTodosHandler;
    private readonly ObtenerRepartidorPorIdHandler _obtenerPorIdHandler;
    private readonly AsignarPedidoHandler _asignarPedidoHandler;
    private readonly ConsultarCargaTrabajoHandler _cargaTrabajoHandler;

    public RepartidoresController(
        CrearRepartidorHandler crearHandler,
        ActualizarRepartidorHandler actualizarHandler,
        CambiarDisponibilidadHandler disponibilidadHandler,
        ObtenerRepartidoresHandler obtenerTodosHandler,
        ObtenerRepartidorPorIdHandler obtenerPorIdHandler,
        AsignarPedidoHandler asignarPedidoHandler,
        ConsultarCargaTrabajoHandler cargaTrabajoHandler)
    {
        _crearHandler = crearHandler;
        _actualizarHandler = actualizarHandler;
        _disponibilidadHandler = disponibilidadHandler;
        _obtenerTodosHandler = obtenerTodosHandler;
        _obtenerPorIdHandler = obtenerPorIdHandler;
        _asignarPedidoHandler = asignarPedidoHandler;
        _cargaTrabajoHandler = cargaTrabajoHandler;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RepartidorDto>>> GetAll([FromQuery] bool? soloDisponibles, CancellationToken ct)
    {
        var repartidores = await _obtenerTodosHandler.HandleAsync(new ObtenerRepartidoresQuery(soloDisponibles), ct);
        return Ok(repartidores);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RepartidorDto>> GetById(int id, CancellationToken ct)
    {
        var repartidor = await _obtenerPorIdHandler.HandleAsync(new ObtenerRepartidorPorIdQuery(id), ct);
        if (repartidor == null)
            return NotFound(new { mensaje = $"No se encontró el repartidor con ID {id}." });

        return Ok(repartidor);
    }

    // RF09: ABM Repartidores (Crear)
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CrearRepartidorCommand command, CancellationToken ct)
    {
        var id = await _crearHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    // RF09: ABM Repartidores (Actualizar)
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] ActualizarRepartidorRequest request, CancellationToken ct)
    {
        var command = new ActualizarRepartidorCommand(id, request.Nombre, request.Telefono, request.Vehiculo);
        await _actualizarHandler.HandleAsync(command, ct);
        return NoContent();
    }

    // RF09: ABM Repartidores (Cambiar Disponibilidad)
    [HttpPatch("{id:int}/disponibilidad")]
    public async Task<ActionResult> SetDisponibilidad(int id, [FromBody] CambiarDisponibilidadRequest request, CancellationToken ct)
    {
        var command = new CambiarDisponibilidadCommand(id, request.Disponible);
        await _disponibilidadHandler.HandleAsync(command, ct);
        return NoContent();
    }

    // RF11: Asignar pedido a repartidor
    [HttpPost("{id:int}/pedidos/{pedidoId:int}")]
    public async Task<ActionResult> AsignarPedido(int id, int pedidoId, CancellationToken ct)
    {
        var command = new AsignarPedidoCommand(id, pedidoId);
        await _asignarPedidoHandler.HandleAsync(command, ct);
        return NoContent();
    }

    // RF10: Carga de trabajo por repartidor (ordenada y filtrada)
    [HttpGet("carga")]
    public async Task<ActionResult<IReadOnlyList<CargaRepartidorDto>>> GetCargaTrabajo(
        [FromQuery] bool? soloDisponibles,
        [FromQuery] bool? soloConCapacidad,
        CancellationToken ct)
    {
        var resultado = await _cargaTrabajoHandler.HandleAsync(new ConsultarCargaTrabajoQuery(soloDisponibles, soloConCapacidad), ct);
        return Ok(resultado);
    }

    // RF10: Conteo agrupado clave-valor (Dictionary<string, int>)
    [HttpGet("conteo-asignaciones")]
    public async Task<ActionResult<Dictionary<string, int>>> GetConteoAsignaciones(CancellationToken ct)
    {
        var conteos = await _cargaTrabajoHandler.ObtenerConteoAsignacionesPorRepartidorAsync(ct);
        return Ok(conteos);
    }

    // RF10: Primer repartidor disponible con menor carga (FirstOrDefault)
    [HttpGet("primer-disponible")]
    public async Task<ActionResult<CargaRepartidorDto>> GetPrimerDisponible(CancellationToken ct)
    {
        var primer = await _cargaTrabajoHandler.ObtenerPrimerDisponibleMenorCargaAsync(ct);
        if (primer == null)
            return NotFound(new { mensaje = "No hay repartidores disponibles en este momento." });

        return Ok(primer);
    }
}

public record ActualizarRepartidorRequest(string Nombre, string Telefono, string Vehiculo);
public record CambiarDisponibilidadRequest(bool Disponible);
