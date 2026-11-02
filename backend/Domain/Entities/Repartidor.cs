namespace Domain.Entities;

using Domain.Enums;
using Domain.Exceptions;

/// <summary>
/// Representa a un repartidor operativo dentro de la jerarquía de usuarios.
/// Responsabilidad: Gestiona su capacidad operativa, disponibilidad, asignación de pedidos, recorridos de colecciones y métricas de carga de trabajo.
/// Mensajes que entiende:
/// - ObtenerRol(): Identificador polimórfico de rol ("Repartidor").
/// - PuedeAsignarseEntregas(): Regla polimórfica que evalúa disponibilidad y cupo disponible frente a CapacidadMaxima.
/// - ActualizarDisponibilidad(disponible) / SetDisponible(disponible): Modifica el estado operativo del repartidor.
/// - ActualizarVehiculo(vehiculo): Actualiza el medio de transporte asignado.
/// - AsignarPedido(pedido) / AgregarPedido(pedido): Asocia un pedido si se satisfacen las invariantes de disponibilidad y carga.
/// - ObtenerCargaTrabajoActiva(): Cantidad de pedidos en curso (no entregados ni cancelados).
/// - TieneCapacidadDisponible(): Indica si puede recibir más entregas.
/// - ObtenerPedidosEntregados(): Recorrido de colección de filtrado por condición (cumple).
/// - ObtenerPedidosNoEntregados(): Recorrido de colección de filtrado complementario (rechazo).
/// - ObtenerResumenPedidos(): Recorrido de colección de transformación/proyección (map).
/// - BuscarPrimerPedidoUrgente(): Recorrido de búsqueda del primer elemento que cumple condición.
/// - ConteoPedidosPorEstado(): Estructura clave-valor (Dictionary) con conteo de ocurrencias de estado.
/// - ObtenerPedidosOrdenadosPorFechaPactada(): Salida ordenada por criterio explícito determinístico.
/// </summary>
public class Repartidor : Usuario
{
    private readonly List<Pedido> _pedidos = new();

    public const int CapacidadMaximaPorDefecto = 5;

    public string Vehiculo { get; private set; } = null!;
    public bool Disponible { get; private set; }
    public int CapacidadMaxima { get; private set; } = CapacidadMaximaPorDefecto;
    public IReadOnlyCollection<Pedido> Pedidos => _pedidos.AsReadOnly();

    // Constructor privado sin parámetros para EF Core
    private Repartidor() { }

    public Repartidor(
        string nombre,
        string email,
        string telefono,
        string passwordHash,
        string vehiculo,
        bool disponible = true,
        int capacidadMaxima = CapacidadMaximaPorDefecto)
        : base(nombre, email, telefono, passwordHash)
    {
        if (string.IsNullOrWhiteSpace(vehiculo))
            throw new DomainException("El vehículo del repartidor no puede estar vacío.");

        if (capacidadMaxima <= 0)
            throw new DomainException("La capacidad máxima debe ser mayor a cero.");

        Vehiculo = vehiculo.Trim();
        Disponible = disponible;
        CapacidadMaxima = capacidadMaxima;
    }

    // Constructor para hidratación y pruebas unitarias
    public Repartidor(
        int id,
        string nombre,
        string email,
        string telefono,
        string passwordHash,
        string vehiculo,
        bool disponible = true,
        int capacidadMaxima = CapacidadMaximaPorDefecto)
        : this(nombre, email, telefono, passwordHash, vehiculo, disponible, capacidadMaxima)
    {
        Id = id;
    }

    // Polimorfismo justificado
    public override string ObtenerRol() => "Repartidor";
    public override bool PuedeAsignarseEntregas() => TieneCapacidadDisponible();

    public void ActualizarDisponibilidad(bool disponible)
    {
        Disponible = disponible;
    }

    public void SetDisponible(bool disponible) => ActualizarDisponibilidad(disponible);

    public void ActualizarVehiculo(string vehiculo)
    {
        if (string.IsNullOrWhiteSpace(vehiculo))
            throw new DomainException("El vehículo del repartidor no puede estar vacío.");

        Vehiculo = vehiculo.Trim();
    }

    public void AsignarPedido(Pedido pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        if (!Disponible)
            throw new DomainException("No se puede asignar un pedido a un repartidor que no está disponible.");

        if (pedido.EstaFinalizado)
            throw new DomainException("No se puede asignar un pedido que ya ha sido entregado o cancelado.");

        if (!TieneCapacidadDisponible())
            throw new DomainException($"El repartidor ha alcanzado su capacidad máxima operativa ({CapacidadMaxima} pedidos).");

        _pedidos.Add(pedido);
        pedido.AsignarRepartidor(this);
    }

    public void AgregarPedido(Pedido pedido) => AsignarPedido(pedido);

    public int ObtenerCargaTrabajoActiva()
        => _pedidos.Count(p => !p.EstaFinalizado);

    public bool TieneCapacidadDisponible()
        => Disponible && ObtenerCargaTrabajoActiva() < CapacidadMaxima;

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
