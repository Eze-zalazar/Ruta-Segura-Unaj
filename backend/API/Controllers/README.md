# API / Controllers

Los controllers **solo traducen HTTP**. Reciben el request, invocan el
handler y devuelven el código de estado que corresponda.

Lo que **no** va en un controller:
- Lógica de negocio (va en la entidad)
- Orquestación (va en el handler)
- `try/catch` (va en el ExceptionMiddleware)
- Validación de entrada (la resuelve `[ApiController]` con DataAnnotations)
- Acceso directo al `DbContext`

## Rutas

Sustantivos en plural, jerarquía de recursos, **sin verbos en la URL**:

    GET    /api/subastas
    GET    /api/subastas/5
    POST   /api/subastas
    PUT    /api/subastas/5
    DELETE /api/subastas/5
    POST   /api/subastas/5/pujas        ← subrecurso

El verbo lo pone HTTP, no la ruta.

Ejemplo de archivo: `SubastasController.cs`
