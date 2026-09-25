# Application / Interfaces / Services

Contratos de los **servicios que el caso de uso necesita** para funcionar:
notificaciones, usuario actual, cache, tokens.

Aca tambien viven los contratos genericos de handler, si se usan
(`ICommandHandler`, `IQueryHandler`).

Ejemplo de archivo: `INotificador.cs`, `ICurrentUserService.cs`
