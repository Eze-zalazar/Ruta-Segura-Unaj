namespace Application.UseCases.Seguimiento.ConsultarHistorialEntregas;

public record ConsultarHistorialEntregasQuery(
    bool? SoloEntregados = null,
    bool? SoloConIncidencias = null,
    int? RepartidorId = null);
