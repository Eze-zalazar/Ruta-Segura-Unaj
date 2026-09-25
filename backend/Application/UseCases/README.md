# Application / UseCases

Los **casos de uso** del sistema. Una carpeta por operación, con su
Command o Query y su Handler **juntos**.

    UseCases/
    └── Subastas/
        ├── CrearSubasta/
        │   ├── CrearSubastaCommand.cs      ← solo datos de entrada
        │   └── CrearSubastaHandler.cs      ← orquesta el caso de uso
        └── ObtenerSubastas/
            ├── ObtenerSubastasQuery.cs
            └── ObtenerSubastasHandler.cs

Se organiza **por caso de uso**, no por tipo de archivo. Así se abre una
carpeta y está todo lo de esa operación, en vez de saltar entre
`Commands/`, `Queries/` y `Handlers/`.

## Command vs Query

- **Command** — escribe. Cuida las reglas de negocio. Devuelve un id, o nada.
- **Query** — lee. Cuida la performance. Devuelve un DTO y nunca modifica nada.

## Qué va en un Handler

Orquesta: pide datos a los repositorios, llama a los métodos de la entidad
y confirma con `IUnitOfWork`. **No** valida reglas de negocio — eso vive en
la entidad — y no sabe de HTTP ni de SQL.
