# Domain / Entities

Las **entidades del negocio**. Son el corazón del sistema.

Reglas:
- Setters `private`: nadie las deja en estado inválido desde afuera.
- El constructor valida las invariantes.
- Los cambios de estado pasan por métodos con nombre del negocio
  (`Cerrar()`, `DescontarStock()`), no por asignaciones directas.
- Un constructor `private` sin parámetros para que EF Core pueda materializarlas.
- No heredan de nada del ORM: son POCO.

Ejemplo de archivo: `Subasta.cs`, `Cliente.cs`
