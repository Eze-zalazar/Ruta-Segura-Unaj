# Domain / ValueObjects

Objetos que se definen **por su valor**, no por una identidad.

- No tienen `Id`.
- Son inmutables: si cambia, es otro objeto.
- Centralizan una invariante en un solo lugar.

Se mapean con `OwnsOne` en la configuracion de EF: quedan como columnas
comunes de la tabla de la entidad que los contiene.

Ejemplo de archivo: `Dinero.cs`, `Email.cs`, `Periodo.cs`
