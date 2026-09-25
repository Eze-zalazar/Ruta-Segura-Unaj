namespace Domain.Entities;

using Domain.Exceptions;

public class Incidencia
{
    public int Id { get; private set; }
    public DateTime FechaHora { get; private set; }
    public string Tipo { get; private set; } = null!;
    public string Descripcion { get; private set; } = null!;
    public bool Resuelta { get; private set; }

    public int PedidoId { get; private set; }
    public Pedido Pedido { get; private set; } = null!;

    // Constructor privado sin parámetros para EF Core
    private Incidencia() { }

    public Incidencia(int pedidoId, string tipo, string descripcion)
    {
        if (pedidoId <= 0)
            throw new DomainException("La incidencia debe estar asociada a un pedido válido.");

        if (string.IsNullOrWhiteSpace(tipo))
            throw new DomainException("El tipo de incidencia no puede estar vacío.");

        if (string.IsNullOrWhiteSpace(descripcion))
            throw new DomainException("La descripción de la incidencia no puede estar vacía.");

        PedidoId = pedidoId;
        FechaHora = DateTime.UtcNow;
        Tipo = tipo.Trim();
        Descripcion = descripcion.Trim();
        Resuelta = false;
    }

    public void MarcarResuelta()
    {
        Resuelta = true;
    }
}
