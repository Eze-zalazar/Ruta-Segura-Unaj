namespace API.Controllers;

using Application.DTOs;
using Application.UseCases.Repartidores.ActualizarRepartidor;
using Application.UseCases.Repartidores.CambiarDisponibilidad;
using Application.UseCases.Repartidores.CrearRepartidor;
using Application.UseCases.Repartidores.ObtenerRepartidores;
using Application.UseCases.Repartidores.ObtenerRepartidorPorId;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RepartidoresController : ControllerBase
{
    private readonly CrearRepartidorHandler _crearHandler;
    private readonly ActualizarRepartidorHandler _actualizarHandler;
    private readonly CambiarDisponibilidadHandler _disponibilidadHandler;
    private readonly ObtenerRepartidoresHandler _obtenerTodosHandler;
    private readonly ObtenerRepartidorPorIdHandler _obtenerPorIdHandler;

    public RepartidoresController(
        CrearRepartidorHandler crearHandler,
        ActualizarRepartidorHandler actualizarHandler,
        CambiarDisponibilidadHandler disponibilidadHandler,
        ObtenerRepartidoresHandler obtenerTodosHandler,
        ObtenerRepartidorPorIdHandler obtenerPorIdHandler)
    {
        _crearHandler = crearHandler;
        _actualizarHandler = actualizarHandler;
        _disponibilidadHandler = disponibilidadHandler;
        _obtenerTodosHandler = obtenerTodosHandler;
        _obtenerPorIdHandler = obtenerPorIdHandler;
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

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CrearRepartidorCommand command, CancellationToken ct)
    {
        var id = await _crearHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] ActualizarRepartidorRequest request, CancellationToken ct)
    {
        var command = new ActualizarRepartidorCommand(id, request.Nombre, request.Telefono, request.Vehiculo);
        await _actualizarHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/disponibilidad")]
    public async Task<ActionResult> SetDisponibilidad(int id, [FromBody] CambiarDisponibilidadRequest request, CancellationToken ct)
    {
        var command = new CambiarDisponibilidadCommand(id, request.Disponible);
        await _disponibilidadHandler.HandleAsync(command, ct);
        return NoContent();
    }
}

public record ActualizarRepartidorRequest(string Nombre, string Telefono, string Vehiculo);
public record CambiarDisponibilidadRequest(bool Disponible);
