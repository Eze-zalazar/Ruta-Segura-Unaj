namespace Application.Mappings;

using Application.DTOs;
using Domain.Entities;

public static class RepartidorMappings
{
    public static RepartidorDto ToDto(this Repartidor r)
        => new(r.Id, r.Nombre, r.Email, r.Telefono, r.Vehiculo, r.Disponible);
}
