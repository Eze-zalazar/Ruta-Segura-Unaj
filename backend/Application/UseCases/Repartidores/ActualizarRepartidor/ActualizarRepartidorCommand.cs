namespace Application.UseCases.Repartidores.ActualizarRepartidor;

public record ActualizarRepartidorCommand(
    int Id,
    string Nombre,
    string Telefono,
    string Vehiculo);
