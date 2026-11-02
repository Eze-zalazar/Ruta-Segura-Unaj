namespace Domain.Entities;

using Domain.Exceptions;

/// <summary>
/// Representa a un coordinador o encargado de logística dentro de la jerarquía de usuarios.
/// Responsabilidad: Encapsula el sector organizativo del encargado y define la regla polimórfica de que no recibe entregas.
/// Mensajes que entiende:
/// - ActualizarSector(sector): Asigna un nuevo sector de supervisión validando invariantes.
/// - ObtenerRol(): Identificador polimórfico de rol ("Encargado").
/// - PuedeAsignarseEntregas(): Devuelve false incondicionalmente por regla de negocio.
/// </summary>
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
