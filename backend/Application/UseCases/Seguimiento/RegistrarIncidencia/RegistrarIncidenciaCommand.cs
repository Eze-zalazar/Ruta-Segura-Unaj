namespace Application.UseCases.Seguimiento.RegistrarIncidencia;

public record RegistrarIncidenciaCommand(int PedidoId, string Tipo, string Descripcion);
