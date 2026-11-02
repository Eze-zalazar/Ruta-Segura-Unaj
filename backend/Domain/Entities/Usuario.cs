namespace Domain.Entities;

using Domain.Exceptions;

/// <summary>
/// Clase base abstracta para la jerarquía de usuarios del sistema (LSP).
/// Responsabilidad: Encapsula identidad, credenciales y autenticación básica, definiendo el contrato polimórfico de roles y aptitud operativa.
/// Mensajes que entiende:
/// - ActualizarDatosBasicos(nombre, telefono): Modifica datos de contacto básicos validando invariantes.
/// - ActualizarPassword(nuevoPasswordHash): Modifica el hash de la contraseña.
/// - ObtenerRol(): Método abstracto que devuelve la denominación del rol.
/// - PuedeAsignarseEntregas(): Método abstracto que determina polimórficamente si el usuario es sujeto de asignación de pedidos.
/// </summary>
public abstract class Usuario
{
    public int Id { get; protected set; }
    public string Nombre { get; protected set; } = null!;
    public string Email { get; protected set; } = null!;
    public string Telefono { get; protected set; } = null!;
    public string PasswordHash { get; protected set; } = null!;

    // Constructor protegido sin parámetros para EF Core
    protected Usuario() { }

    protected Usuario(string nombre, string email, string telefono, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del usuario no puede estar vacío.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("El email del usuario es inválido.");

        if (string.IsNullOrWhiteSpace(telefono))
            throw new DomainException("El teléfono no puede estar vacío.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("El password hash no puede estar vacío.");

        Nombre = nombre.Trim();
        Email = email.Trim().ToLowerInvariant();
        Telefono = telefono.Trim();
        PasswordHash = passwordHash;
    }

    public void ActualizarDatosBasicos(string nombre, string telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del usuario no puede estar vacío.");

        if (string.IsNullOrWhiteSpace(telefono))
            throw new DomainException("El teléfono no puede estar vacío.");

        Nombre = nombre.Trim();
        Telefono = telefono.Trim();
    }

    public void ActualizarPassword(string nuevoPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(nuevoPasswordHash))
            throw new DomainException("El nuevo password no puede estar vacío.");

        PasswordHash = nuevoPasswordHash;
    }

    // Comportamiento polimórfico justificado
    public abstract string ObtenerRol();
    public abstract bool PuedeAsignarseEntregas();
}
