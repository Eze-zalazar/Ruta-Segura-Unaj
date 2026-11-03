# Application / DTOs

Los objetos que **salen** hacia la capa de presentación.

Las entidades del dominio nunca cruzan hacia afuera: si se exponen,
cualquier cambio interno rompe la API pública, y además se filtran
campos que no se querían exponer.

Ejemplo de archivo: `SubastaDto.cs`
