# Infrastructure / Repositories

Las implementaciones de las interfaces declaradas en
`Application/Interfaces/Persistence`.

**Único punto de contacto con EF Core** en todo el sistema.

Regla: **nunca llamar a `SaveChanges` acá.** El repositorio prepara los
cambios; el caso de uso decide cuándo se confirman, vía `IUnitOfWork`.
Si cada método persiste por su cuenta, se rompe la atomicidad.

Ejemplo de archivo: `SubastaRepository.cs`
