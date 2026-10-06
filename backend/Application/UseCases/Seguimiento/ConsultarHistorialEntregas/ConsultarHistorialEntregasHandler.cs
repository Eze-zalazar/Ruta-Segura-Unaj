namespace Application.UseCases.Seguimiento.ConsultarHistorialEntregas;

using Application.DTOs;
using Application.Interfaces.Persistence;
using Domain.Entities;
using Domain.Enums;

public class ConsultarHistorialEntregasHandler
{
    private readonly IPedidoRepository _pedidoRepository;

    public ConsultarHistorialEntregasHandler(IPedidoRepository pedidoRepository)
    {
        _pedidoRepository = pedidoRepository;
    }

    /// <summary>
    /// RF14: Consulta de historial y trazabilidad con los 6 recorridos LINQ obligatorios:
    /// a) Filtrar entregas concretadas exitosamente (Where)
    /// b) Filtrar con incidencias / no entregados (Where condicional/negado)
    /// c) Mapear a DTOs (Select)
    /// d) Obtener la incidencia más reciente (OrderByDescending + FirstOrDefault)
    /// f) Ordenamiento cronológico explícito (OrderByDescending)
    /// </summary>
    public async Task<IReadOnlyList<HistorialEntregaDto>> HandleAsync(ConsultarHistorialEntregasQuery? query = null, CancellationToken cancellationToken = default)
    {
        var pedidos = await _pedidoRepository.GetAllAsync(cancellationToken);
        IEnumerable<Pedido> resultado = pedidos;

        // a) Filtrar entregas concretadas exitosamente (Where)
        if (query?.SoloEntregados == true)
        {
            resultado = resultado.Where(p => p.Estado == EstadoPedido.Entregado);
        }

        // b) Filtrar pedidos con incidencias o no entregados (Where condicional/negado)
        if (query?.SoloConIncidencias == true)
        {
            resultado = resultado.Where(p => p.Incidencias.Any() || p.Estado == EstadoPedido.ConInconveniente);
        }

        if (query?.RepartidorId.HasValue == true)
        {
            resultado = resultado.Where(p => p.RepartidorId == query.RepartidorId.Value);
        }

        // f) Ordenamiento cronológico explícito por fecha
        resultado = resultado.OrderByDescending(p => p.FechaPactada);

        // c) Mapear a HistorialEntregaDto e IncidenciaDto
        return resultado.Select(p =>
        {
            var incidenciasDto = p.Incidencias
                .OrderByDescending(i => i.FechaHora)
                .Select(i => new IncidenciaDto(i.Id, i.PedidoId, i.Tipo, i.Descripcion, i.FechaHora, i.Resuelta))
                .ToList();

            // d) Obtener el evento o incidencia más reciente del pedido
            var ultimaIncidencia = incidenciasDto.FirstOrDefault();

            return new HistorialEntregaDto(
                p.Id,
                p.Descripcion,
                p.Estado,
                p.FechaPactada,
                p.RepartidorId,
                p.Repartidor?.Nombre,
                p.Cliente?.Nombre,
                incidenciasDto,
                ultimaIncidencia);
        }).ToList();
    }

    /// <summary>
    /// e) Estructura clave-valor obligatoria: Diccionario que agrupa y cuenta incidencias por tipo/motivo (GroupBy + ToDictionary).
    /// </summary>
    public async Task<Dictionary<string, int>> ObtenerConteoIncidenciasPorTipoAsync(CancellationToken cancellationToken = default)
    {
        var pedidos = await _pedidoRepository.GetAllAsync(cancellationToken);

        // e) Agrupamiento y conteo clave-valor
        return pedidos
            .SelectMany(p => p.Incidencias)
            .GroupBy(i => i.Tipo)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// e) Alternativa clave-valor: Entregas concretadas por repartidor.
    /// </summary>
    public async Task<Dictionary<string, int>> ObtenerEntregasConcretadasPorRepartidorAsync(CancellationToken cancellationToken = default)
    {
        var pedidos = await _pedidoRepository.GetAllAsync(cancellationToken);

        return pedidos
            .Where(p => p.Estado == EstadoPedido.Entregado && p.Repartidor != null)
            .GroupBy(p => p.Repartidor!.Nombre)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
