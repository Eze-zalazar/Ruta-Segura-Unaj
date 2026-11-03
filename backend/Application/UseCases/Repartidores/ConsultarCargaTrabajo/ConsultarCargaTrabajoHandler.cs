namespace Application.UseCases.Repartidores.ConsultarCargaTrabajo;

using Application.DTOs;
using Application.Interfaces.Persistence;
using Domain.Entities;

public class ConsultarCargaTrabajoHandler
{
    private readonly IRepartidorRepository _repartidorRepository;

    public ConsultarCargaTrabajoHandler(IRepartidorRepository repartidorRepository)
    {
        _repartidorRepository = repartidorRepository;
    }

    /// <summary>
    /// RF10: Consulta y cálculo de carga de trabajo aplicando recorridos LINQ obligatorios de cátedra:
    /// a) Filtrar repartidores disponibles (Where)
    /// b) Filtrar con capacidad / no disponibles (Where negado)
    /// c) Mapeo a DTOs (Select)
    /// f) Salida ordenada con criterio explícito (OrderBy)
    /// </summary>
    public async Task<IReadOnlyList<CargaRepartidorDto>> HandleAsync(ConsultarCargaTrabajoQuery? query = null, CancellationToken cancellationToken = default)
    {
        var repartidores = await _repartidorRepository.GetAllAsync(cancellationToken);
        IEnumerable<Repartidor> resultado = repartidores;

        // a) Filtrar repartidores disponibles (Where)
        if (query?.SoloDisponibles == true)
        {
            resultado = resultado.Where(r => r.Disponible);
        }

        // b) Filtrar los que NO cumplen o tienen carga completa (Where negado)
        if (query?.SoloConCapacidad == true)
        {
            resultado = resultado.Where(r => r.Disponible && r.TieneCapacidadDisponible());
        }

        // f) Salida ordenada explícita por carga de trabajo y nombre (OrderBy)
        resultado = resultado.OrderBy(r => r.ObtenerCargaTrabajoActiva()).ThenBy(r => r.Nombre);

        // c) Mapeo a DTOs (Select)
        return resultado.Select(r => new CargaRepartidorDto(
            r.Id,
            r.Nombre,
            r.Vehiculo,
            r.Disponible,
            r.ObtenerCargaTrabajoActiva(),
            r.CapacidadMaxima,
            r.TieneCapacidadDisponible()
        )).ToList();
    }

    /// <summary>
    /// d) Obtener el primer repartidor disponible con menor carga (FirstOrDefault).
    /// </summary>
    public async Task<CargaRepartidorDto?> ObtenerPrimerDisponibleMenorCargaAsync(CancellationToken cancellationToken = default)
    {
        var repartidores = await _repartidorRepository.GetAllAsync(cancellationToken);

        // d) Primer elemento que cumple condición de disponibilidad y menor carga operativa
        var primer = repartidores
            .Where(r => r.Disponible && r.TieneCapacidadDisponible())
            .OrderBy(r => r.ObtenerCargaTrabajoActiva())
            .ThenBy(r => r.Nombre)
            .FirstOrDefault();

        return primer == null ? null : new CargaRepartidorDto(
            primer.Id,
            primer.Nombre,
            primer.Vehiculo,
            primer.Disponible,
            primer.ObtenerCargaTrabajoActiva(),
            primer.CapacidadMaxima,
            primer.TieneCapacidadDisponible());
    }

    /// <summary>
    /// e) Estructura clave-valor obligatoria: Diccionario con conteo de asignaciones por repartidor (ToDictionary).
    /// </summary>
    public async Task<Dictionary<string, int>> ObtenerConteoAsignacionesPorRepartidorAsync(CancellationToken cancellationToken = default)
    {
        var repartidores = await _repartidorRepository.GetAllAsync(cancellationToken);

        // e) Estructura clave-valor (Dictionary<string, int>)
        return repartidores.ToDictionary(
            r => r.Nombre,
            r => r.ObtenerCargaTrabajoActiva());
    }
}
