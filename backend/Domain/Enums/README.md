# Domain / Enums

Estados y clasificaciones del negocio.

EF Core los mapea de forma nativa. Para que en la base se guarden como
texto legible en vez de un número:

    modelBuilder.Entity<X>().Property(x => x.Estado)
        .HasConversion<string>().HasMaxLength(20);

Ejemplo de archivo: `EstadoSubasta.cs`
