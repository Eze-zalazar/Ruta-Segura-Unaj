namespace Application.UseCases.Repartidores.CrearRepartidor;

public record CrearRepartidorCommand(
    string Nombre,
    string Email,
    string Telefono,
    string Password,
    string Vehiculo,
    bool Disponible = true);
