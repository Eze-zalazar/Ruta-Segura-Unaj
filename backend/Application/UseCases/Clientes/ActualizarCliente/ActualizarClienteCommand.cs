namespace Application.UseCases.Clientes.ActualizarCliente;

public record ActualizarClienteCommand(
    int Id,
    string Nombre,
    string Telefono,
    string Direccion,
    string? Referencia = null);
