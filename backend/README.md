# Template — Clean Architecture con .NET 8

Estructura base para los trabajos prácticos de **Proyecto de Software**.

Cuatro proyectos, las referencias ya configuradas y las carpetas con un
`README.md` que explica qué va en cada una. **No trae clases implementadas**:
es el andamiaje sobre el que se construye.

---

## Levantarlo

```bash
dotnet build
dotnet run --project API
```

Swagger queda en `https://localhost:7xxx/swagger` (el puerto está en
`API/Properties/launchSettings.json`).

---

## La regla de dependencias

```
        API ──────────┐
         │            │
         ▼            ▼
    Application ──► Domain ◄────── Infrastructure
                   (el centro)
```

| Proyecto | Referencia a | Porque usa |
|---|---|---|
| **Domain** | *nadie* | Es el centro. Ni siquiera tiene paquetes NuGet |
| **Application** | Domain | Las entidades, y lanza `DomainException` |
| **Infrastructure** | Application, Domain | Implementa las interfaces y mapea las entidades |
| **API** | Application, Infrastructure, Domain | Handlers, registro de DI, y las excepciones en el middleware |

Todas las flechas apuntan al centro. **Domain no referencia a ningún proyecto.**

### Comprobalo

```bash
dotnet add Domain reference Infrastructure
```

Tiene que fallar con «se ha detectado una dependencia circular». La arquitectura
no es una convención que hay que recordar: **la hace cumplir el compilador**.

---

## Estructura

```
Template/
├── Domain/                          ← sin dependencias ni paquetes
│   ├── Entities/                       entidades del negocio
│   ├── ValueObjects/                   objetos definidos por su valor
│   ├── Enums/                          estados y clasificaciones
│   └── Exceptions/                     DomainException
│
├── Application/                     ← casos de uso
│   ├── DTOs/                           lo que sale hacia afuera
│   ├── Interfaces/
│   │   ├── Persistence/                IRepository, IUnitOfWork
│   │   └── Services/                   INotificador, ICurrentUserService
│   ├── Mappings/                       entidad → DTO
│   └── UseCases/                       una carpeta por caso de uso
│
├── Infrastructure/                  ← el detalle técnico
│   ├── Data/                           DbContext, UnitOfWork, Migrations
│   │   └── Configurations/             Fluent API, una por entidad
│   ├── Repositories/                   único contacto con EF Core
│   └── Services/                       mail, caché, tokens
│
└── API/                             ← entrada HTTP
    ├── Controllers/
    ├── Middleware/
    └── Program.cs                      composition root
```

Cada carpeta tiene su propio `README.md` con qué va adentro y por qué.

---

## Los primeros pasos

1. **Una entidad** en `Domain/Entities/`, con setters privados y las
   validaciones en el constructor.
2. **`DomainException`** en `Domain/Exceptions/`.
3. **La interfaz del repositorio** en `Application/Interfaces/Persistence/`
   — sin exponer `SaveChanges`.
4. **Un caso de uso** en `Application/UseCases/<Entidad>/<Operacion>/`,
   con su Command y su Handler juntos.
5. **El `DbContext`** en `Infrastructure/Data/` y el repositorio en
   `Infrastructure/Repositories/`.
6. **El `ExceptionMiddleware`** en `API/Middleware/`.
7. **El controller** en `API/Controllers/`, que solo traduce HTTP.
8. **Registrar todo** en `Program.cs`.

---

## Paquetes que van a necesitar

EF Core va **solo en Infrastructure**. Domain no lleva ninguno.

```bash
dotnet add Infrastructure package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.11
dotnet add Infrastructure package Microsoft.EntityFrameworkCore.Design --version 8.0.11
dotnet add API package Microsoft.EntityFrameworkCore.Design --version 8.0.11
```

Fijen **una sola versión** para toda la solución.

---

## Migrations

El `DbContext` vive en Infrastructure pero la app arranca en API, así que
hacen falta los dos flags:

```bash
dotnet ef migrations add Inicial --project Infrastructure --startup-project API --output-dir Data/Migrations
```

```bash
dotnet ef database update --project Infrastructure --startup-project API
```

---

## Sobre la connection string

Mientras apunte a una **base local** con datos de prueba y sin credenciales
reales, está bien dejarla en `appsettings.Development.json`: así el proyecto
arranca con un `dotnet run` sin configurar nada.

Si alguna vez apunta a un servidor real con usuario y contraseña, va en
User Secrets:

```bash
dotnet user-secrets init --project API
```

La regla práctica: **¿le sirve a alguien que la encuentre en GitHub?**
Si la respuesta es sí, no va al repositorio.
