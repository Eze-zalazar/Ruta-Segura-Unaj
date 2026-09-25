namespace Domain.Entities;

using Domain.Enums;
using Domain.Exceptions;

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

    public void AsignarRepartidor(Repartidor repartidor)
    {
        ArgumentNullException.ThrowIfNull(repartidor);

        if (!repartidor.Disponible)
            throw new DomainException("El repartidor no se encuentra disponible.");

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

    public void CambiarEstado(EstadoPedido nuevoEstado)
    {
        Estado = nuevoEstado;
    }

    public void RegistrarIncidencia(Incidencia incidencia)
    {
        ArgumentNullException.ThrowIfNull(incidencia);
        _incidencias.Add(incidencia);
        Estado = EstadoPedido.ConInconveniente;
    }
}
