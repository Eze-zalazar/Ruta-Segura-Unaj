# Domain / Exceptions

Excepciones de **negocio**: se lanzan cuando se viola una regla del dominio.

El dominio no conoce HTTP. Lanza la excepción, y el `ExceptionMiddleware`
de la capa de presentación la traduce al código de estado que corresponda.

Ejemplo de archivo: `DomainException.cs`

    public class DomainException : Exception
    {
        public DomainException(string mensaje) : base(mensaje) { }
    }
