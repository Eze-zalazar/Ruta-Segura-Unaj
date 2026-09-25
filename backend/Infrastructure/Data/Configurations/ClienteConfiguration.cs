namespace Infrastructure.Data.Configurations;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Telefono)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Direccion)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Referencia)
            .HasMaxLength(250);
    }
}
