namespace Infrastructure.Data.Configurations;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Descripcion)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Observaciones)
            .HasMaxLength(500);

        builder.Property(p => p.Estado)
            .HasConversion<string>()
            .HasMaxLength(25);

        builder.Property(p => p.Prioridad)
            .HasConversion<string>()
            .HasMaxLength(25);

        builder.HasOne(p => p.Cliente)
            .WithMany(c => c.Pedidos)
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Repartidor)
            .WithMany(r => r.Pedidos)
            .HasForeignKey(p => p.RepartidorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Incidencias)
            .WithOne(i => i.Pedido)
            .HasForeignKey(i => i.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
