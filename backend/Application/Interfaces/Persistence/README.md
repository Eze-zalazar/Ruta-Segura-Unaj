# Application / Interfaces / Persistence

Los **contratos de acceso a datos**. Se declaran aca; se implementan en Infrastructure.

Esa es la inversion de dependencias: Application no sabe que existe EF Core.

Regla importante: **el repositorio nunca expone `SaveChanges`.** Confirmar la
transaccion es decision del caso de uso, y para eso esta `IUnitOfWork`.

Ejemplo de archivo: `ISubastaRepository.cs`, `IUnitOfWork.cs`
