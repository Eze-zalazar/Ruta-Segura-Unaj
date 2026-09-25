namespace Domain.Entities;

using Domain.Exceptions;

public class Encargado : Usuario
{
    public string Sector { get; private set; } = null!;

    // Constructor privado sin parámetros para EF Core
    private Encargado() { }

    public Encargado(string nombre, string email, string telefono, string passwordHash, string sector)
        : base(nombre, email, telefono, passwordHash)
    {
        if (string.IsNullOrWhiteSpace(sector))
            throw new DomainException("El sector del encargado no puede estar vacío.");

        Sector = sector.Trim();
    }

    public void ActualizarSector(string sector)
    {
        if (string.IsNullOrWhiteSpace(sector))
            throw new DomainException("El sector del encargado no puede estar vacío.");

        Sector = sector.Trim();
    }

    public override string ObtenerRol() => "Encargado";
    public override bool PuedeAsignarseEntregas() => false;
}
