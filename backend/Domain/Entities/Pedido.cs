namespace Domain.Entities;

using Domain.Enums;
using Domain.Exceptions;

/// <summary>
/// Representa un pedido dentro del sistema logístico.
/// Responsabilidad: Encapsula el ciclo de vida, la asignación a repartidores, las invariantes de transición de estado y el registro de incidencias asociadas.
/// Mensajes que entiende:
/// - AsignarRepartidor(repartidor): Vincula el pedido a un repartidor disponible y transiciona a Asignado.
/// - DesasignarRepartidor(): Remueve la asignación del repartidor y retrocede a Pendiente si aplicaba.
/// - ActualizarDatos(descripcion, fechaPactada, prioridad, observaciones): Modifica detalles descriptivos si no fue entregado ni cancelado.
/// - MarcarEnCamino(): Pone en despacho un pedido previamente asignado.
/// - RegistrarEntrega(): Concluye exitosamente la entrega de un pedido en tránsito.
/// - Cancelar(): Finaliza el pedido por anulación.
/// - CambiarEstado(nuevoEstado): Canaliza transiciones semánticas controladas.
/// - RegistrarIncidencia(incidencia): Incorpora un inconveniente y pasa a estado ConInconveniente.
/// </summary>
public class Pedido
{
    private readonly List<Incidencia> _incidencias = new();

    public int Id { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime FechaPactada { get; private set; }
    public PrioridadPedido Prioridad { get; private set; }
    public EstadoPedido Estado { get; private set; }
    public string Descripcion { get; private set; } = null!;
    public string? Observaciones { get; private set; }

    public int ClienteId { get; private set; }
    public Cliente Cliente { get; private set; } = null!;

    public int? RepartidorId { get; private set; }
    public Repartidor? Repartidor { get; private set; }

    public IReadOnlyCollection<Incidencia> Incidencias => _incidencias.AsReadOnly();

    /// <summary>
    /// Encapsula el conocimiento del ciclo de vida: indica si el pedido concluyó su ciclo operativo.
    /// </summary>
    public bool EstaFinalizado => Estado == EstadoPedido.Entregado || Estado == EstadoPedido.Cancelado;

    // Constructor privado sin parámetros para EF Core
    private Pedido() { }

    public Pedido(
        int clienteId,
        DateTime fechaPactada,
        PrioridadPedido prioridad,
        string descripcion,
        string? observaciones = null)
    {
        if (clienteId <= 0)
            throw new DomainException("El pedido debe estar asociado a un cliente válido.");

        if (string.IsNullOrWhiteSpace(descripcion))
            throw new DomainException("La descripción del pedido no puede estar vacía.");

        if (fechaPactada == default)
            throw new DomainException("La fecha pactada no es válida.");

        ClienteId = clienteId;
        FechaCreacion = DateTime.UtcNow;
        FechaPactada = fechaPactada;
        Prioridad = prioridad;
        Estado = EstadoPedido.Pendiente;
        Descripcion = descripcion.Trim();
        Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
    }

    // Constructor para hidratación y pruebas unitarias
    public Pedido(
        int id,
        int clienteId,
        DateTime fechaPactada,
        PrioridadPedido prioridad,
        string descripcion,
        string? observaciones = null,
        EstadoPedido estado = EstadoPedido.Pendiente)
        : this(clienteId, fechaPactada, prioridad, descripcion, observaciones)
    {
        Id = id;
        Estado = estado;
    }

    public void AsignarRepartidor(Repartidor repartidor)
    {
        ArgumentNullException.ThrowIfNull(repartidor);

        if (!repartidor.Disponible)
            throw new DomainException("El repartidor no se encuentra disponible.");

        if (EstaFinalizado)
            throw new DomainException("No se puede asignar un pedido que ya ha sido entregado o cancelado.");

        RepartidorId = repartidor.Id;
        Repartidor = repartidor;
        Estado = EstadoPedido.Asignado;
    }

    public void DesasignarRepartidor()
    {
        RepartidorId = null;
        Repartidor = null;
        if (Estado == EstadoPedido.Asignado)
            Estado = EstadoPedido.Pendiente;
    }

    public void ActualizarDatos(
        string descripcion,
        DateTime fechaPactada,
        PrioridadPedido prioridad,
        string? observaciones = null)
    {
        if (Estado == EstadoPedido.Entregado)
            throw new DomainException("No se puede modificar un pedido que ya ha sido entregado.");

        if (Estado == EstadoPedido.Cancelado)
            throw new DomainException("No se puede modificar un pedido cancelado.");

        if (string.IsNullOrWhiteSpace(descripcion))
            throw new DomainException("La descripción del pedido no puede estar vacía.");

        if (fechaPactada == default)
            throw new DomainException("La fecha pactada no es válida.");

        Descripcion = descripcion.Trim();
        FechaPactada = fechaPactada;
        Prioridad = prioridad;
        Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
    }

    public void MarcarEnCamino()
    {
        if (Estado == EstadoPedido.Cancelado)
            throw new DomainException("No se puede despachar un pedido cancelado.");

        if (Estado == EstadoPedido.Entregado)
            throw new DomainException("El pedido ya ha sido entregado.");

        if (Estado == EstadoPedido.Pendiente || RepartidorId == null)
            throw new DomainException("El pedido debe tener un repartidor asignado antes de ponerse en camino.");

        Estado = EstadoPedido.EnCamino;
    }

    public void RegistrarEntrega()
    {
        if (Estado == EstadoPedido.Cancelado)
            throw new DomainException("No se puede registrar la entrega de un pedido cancelado.");

        if (Estado == EstadoPedido.Entregado)
            throw new DomainException("El pedido ya ha sido entregado previamente.");

        if (Estado == EstadoPedido.Pendiente || RepartidorId == null)
            throw new DomainException("El pedido debe ser asignado y despachado antes de registrar la entrega.");

        Estado = EstadoPedido.Entregado;
    }

    public void Cancelar()
    {
        if (Estado == EstadoPedido.Entregado)
            throw new DomainException("No se puede cancelar un pedido que ya ha sido entregado.");

        if (Estado == EstadoPedido.Cancelado)
            throw new DomainException("El pedido ya se encuentra cancelado.");

        Estado = EstadoPedido.Cancelado;
    }

    public void CambiarEstado(EstadoPedido nuevoEstado)
    {
        if (nuevoEstado == Estado) return;

        switch (nuevoEstado)
        {
            case EstadoPedido.EnCamino:
                MarcarEnCamino();
                break;
            case EstadoPedido.Entregado:
                RegistrarEntrega();
                break;
            case EstadoPedido.Cancelado:
                Cancelar();
                break;
            case EstadoPedido.Asignado when RepartidorId.HasValue:
                Estado = EstadoPedido.Asignado;
                break;
            case EstadoPedido.Pendiente when RepartidorId == null:
                Estado = EstadoPedido.Pendiente;
                break;
            default:
                throw new DomainException($"Transición de estado no permitida desde {Estado} hacia {nuevoEstado} sin cumplir sus invariantes.");
        }
    }

    public void RegistrarIncidencia(Incidencia incidencia)
    {
        ArgumentNullException.ThrowIfNull(incidencia);

        if (Estado == EstadoPedido.Cancelado)
            throw new DomainException("No se pueden registrar incidencias en un pedido cancelado.");

        if (Estado == EstadoPedido.Entregado)
            throw new DomainException("No se pueden registrar incidencias en un pedido que ya fue entregado.");

        _incidencias.Add(incidencia);
        Estado = EstadoPedido.ConInconveniente;
    }
}
