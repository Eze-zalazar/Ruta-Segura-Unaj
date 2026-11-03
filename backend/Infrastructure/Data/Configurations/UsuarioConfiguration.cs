namespace Infrastructure.Data.Configurations;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Telefono)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        // Mapeo TPH (Table per Hierarchy)
        builder.HasDiscriminator<string>("TipoUsuario")
            .HasValue<Encargado>("Encargado")
            .HasValue<Repartidor>("Repartidor");
    }
}

public class EncargadoConfiguration : IEntityTypeConfiguration<Encargado>
{
    public void Configure(EntityTypeBuilder<Encargado> builder)
    {
        builder.Property(e => e.Sector)
            .HasMaxLength(100);
    }
}

public class RepartidorConfiguration : IEntityTypeConfiguration<Repartidor>
{
    public void Configure(EntityTypeBuilder<Repartidor> builder)
    {
        builder.Property(r => r.Vehiculo)
            .HasMaxLength(100);

        builder.Property(r => r.Disponible)
            .HasDefaultValue(true);
    }
}
