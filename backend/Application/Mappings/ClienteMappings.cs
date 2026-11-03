namespace Application.Mappings;

using Application.DTOs;
using Domain.Entities;

public static class ClienteMappings
{
    public static ClienteDto ToDto(this Cliente c)
        => new(c.Id, c.Nombre, c.Telefono, c.Direccion, c.Referencia);
}
