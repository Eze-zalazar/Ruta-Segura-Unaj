namespace Application.UseCases.Repartidores.ConsultarCargaTrabajo;

public record ConsultarCargaTrabajoQuery(
    bool? SoloDisponibles = null,
    bool? SoloConCapacidad = null);
