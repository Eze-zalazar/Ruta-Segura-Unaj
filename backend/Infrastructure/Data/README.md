# Infrastructure / Data

El `DbContext` y todo lo que tiene que ver con la base de datos.

    Data/
    ├── AppDbContext.cs
    ├── UnitOfWork.cs             ← implementa IUnitOfWork
    ├── DbSeeder.cs               ← datos de prueba
    ├── Configurations/           ← Fluent API, una clase por entidad
    └── Migrations/               ← generadas por dotnet ef

Las **migraciones viven acá**, no en la API: describen cómo se materializa
el modelo en el motor concreto, y eso es detalle de infraestructura.

    dotnet ef migrations add Inicial \
      --project Infrastructure --startup-project API \
      --output-dir Data/Migrations
