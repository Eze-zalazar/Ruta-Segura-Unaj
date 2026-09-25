# Application / Mappings

Conversion de **entidad a DTO**, con metodos de extension.

A mano, sin AutoMapper: la conversion explicita se lee mejor y no esconde
que campo viene de donde.

Ejemplo de archivo: `SubastaMappings.cs`

    public static SubastaDto ToDto(this Subasta s)
        => new(s.Id, s.Titulo, ...);
