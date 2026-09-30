namespace Application.UseCases.Pedidos.FiltrarPedidos;

using Domain.Enums;

public record FiltrarPedidosQuery(
    EstadoPedido? Estado = null,
    DateTime? Fecha = null,
    int? ClienteId = null);
