namespace Application.UseCases.Clientes.CrearCliente;

public record CrearClienteCommand(
    string Nombre,
    string Telefono,
    string Direccion,
    string? Referencia = null);
