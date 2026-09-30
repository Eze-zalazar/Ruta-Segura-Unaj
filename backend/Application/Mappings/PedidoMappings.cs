namespace Application.Mappings;

using Application.DTOs;
using Domain.Entities;

public static class PedidoMappings
{
    public static PedidoDto ToDto(this Pedido p)
        => new(
            p.Id,
            p.ClienteId,
            p.Cliente?.Nombre,
            p.FechaCreacion,
            p.FechaPactada,
            p.Prioridad,
            p.Estado,
            p.Descripcion,
            p.Observaciones,
            p.RepartidorId,
            p.Repartidor?.Nombre);
}
