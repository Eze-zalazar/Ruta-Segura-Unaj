namespace Infrastructure.Data.Configurations;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class IncidenciaConfiguration : IEntityTypeConfiguration<Incidencia>
{
    public void Configure(EntityTypeBuilder<Incidencia> builder)
    {
        builder.ToTable("Incidencias");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Tipo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Descripcion)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.Resuelta)
            .HasDefaultValue(false);
    }
}
