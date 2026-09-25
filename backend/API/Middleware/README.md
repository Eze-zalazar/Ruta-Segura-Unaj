# API / Middleware

Middlewares propios del pipeline.

El más importante es el manejador global de excepciones: traduce las
excepciones a códigos HTTP en un solo lugar, para que ningún controller
tenga que repetir `try/catch`.

    DomainException                 → 400 Bad Request
    DbUpdateConcurrencyException    → 409 Conflict
    Exception                       → 500 + log del lado del servidor

Va **primero** en el pipeline: su `try` envuelve la llamada al resto de la
cadena, así que atrapa lo que pase en cualquier middleware o controller
posterior.

Ejemplo de archivo: `ExceptionMiddleware.cs`
