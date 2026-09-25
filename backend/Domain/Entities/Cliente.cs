namespace Domain.Entities;

using Domain.Exceptions;

public class Cliente
{
    private readonly List<Pedido> _pedidos = new();

    public int Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Telefono { get; private set; } = null!;
    public string Direccion { get; private set; } = null!;
    public string? Referencia { get; private set; }
    public IReadOnlyCollection<Pedido> Pedidos => _pedidos.AsReadOnly();

    // Constructor privado sin parámetros para EF Core
    private Cliente() { }

    public Cliente(string nombre, string telefono, string direccion, string? referencia = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del cliente no puede estar vacío.");

        if (string.IsNullOrWhiteSpace(telefono))
            throw new DomainException("El teléfono del cliente no puede estar vacío.");

        if (string.IsNullOrWhiteSpace(direccion))
            throw new DomainException("La dirección del cliente no puede estar vacía.");

        Nombre = nombre.Trim();
        Telefono = telefono.Trim();
        Direccion = direccion.Trim();
        Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim();
    }

    public void ActualizarContacto(string telefono, string direccion, string? referencia = null)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            throw new DomainException("El teléfono del cliente no puede estar vacío.");

        if (string.IsNullOrWhiteSpace(direccion))
            throw new DomainException("La dirección del cliente no puede estar vacía.");

        Telefono = telefono.Trim();
        Direccion = direccion.Trim();
        Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim();
    }

    public void ActualizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del cliente no puede estar vacío.");

        Nombre = nombre.Trim();
    }
}
