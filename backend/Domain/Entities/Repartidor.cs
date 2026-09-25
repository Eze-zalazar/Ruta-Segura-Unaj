namespace Domain.Entities;

using Domain.Enums;
using Domain.Exceptions;

public class Repartidor : Usuario
{
    private readonly List<Pedido> _pedidos = new();

    public string Vehiculo { get; private set; } = null!;
    public bool Disponible { get; private set; }
    public IReadOnlyCollection<Pedido> Pedidos => _pedidos.AsReadOnly();

    // Constructor privado sin parámetros para EF Core
    private Repartidor() { }

    public Repartidor(string nombre, string email, string telefono, string passwordHash, string vehiculo, bool disponible = true)
        : base(nombre, email, telefono, passwordHash)
    {
        if (string.IsNullOrWhiteSpace(vehiculo))
            throw new DomainException("El vehículo del repartidor no puede estar vacío.");

        Vehiculo = vehiculo.Trim();
        Disponible = disponible;
    }

    // Polimorfismo justificado
    public override string ObtenerRol() => "Repartidor";
    public override bool PuedeAsignarseEntregas() => Disponible;

    public void SetDisponible(bool disponible)
    {
        Disponible = disponible;
    }

    public void ActualizarVehiculo(string vehiculo)
    {
        if (string.IsNullOrWhiteSpace(vehiculo))
            throw new DomainException("El vehículo del repartidor no puede estar vacío.");

        Vehiculo = vehiculo.Trim();
    }

    public void AgregarPedido(Pedido pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        if (!Disponible)
            throw new DomainException("No se puede asignar un pedido a un repartidor que no está disponible.");

        _pedidos.Add(pedido);
    }

    // 1. Recorrido: Filtrar los que CUMPLEN una condición (ej: entregados)
    public IEnumerable<Pedido> ObtenerPedidosEntregados()
        => _pedidos.Where(p => p.Estado == EstadoPedido.Entregado);

    // 2. Recorrido: Filtrar los que NO CUMPLEN una condición (ej: no entregados / pendientes)
    public IEnumerable<Pedido> ObtenerPedidosNoEntregados()
        => _pedidos.Where(p => p.Estado != EstadoPedido.Entregado);

    // 3. Recorrido: Transformar cada elemento en otro valor (mapeo a resumen descriptivo)
    public IEnumerable<string> ObtenerResumenPedidos()
        => _pedidos.Select(p => $"Pedido #{p.Id}: {p.Descripcion} ({p.Estado})");

    // 4. Recorrido: Encontrar el PRIMER elemento que cumple una condición
    public Pedido? BuscarPrimerPedidoUrgente()
        => _pedidos.FirstOrDefault(p => p.Prioridad == PrioridadPedido.Urgente && p.Estado != EstadoPedido.Entregado);

    // 5. Estructura clave-valor (Dictionary): Conteo de ocurrencias de situación clave
    public Dictionary<EstadoPedido, int> ConteoPedidosPorEstado()
        => _pedidos.GroupBy(p => p.Estado)
                   .ToDictionary(g => g.Key, g => g.Count());

    // 6. Salida ordenada con criterio explícito (por fecha pactada ascendente)
    public IEnumerable<Pedido> ObtenerPedidosOrdenadosPorFechaPactada()
        => _pedidos.OrderBy(p => p.FechaPactada);
}
