namespace Infrastructure.Data;

using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        // Aplicar migraciones pendientes automáticamente si no están aplicadas
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }

        // Si ya existen usuarios, la base ya fue inicializada
        if (await context.Usuarios.AnyAsync())
        {
            return;
        }

        // 1. Usuarios: Encargado y Repartidores con credenciales hasheadas
        var encargado = new Encargado(
            nombre: "Administrador Logístico",
            email: "admin@rutasegura.com",
            telefono: "11-4000-0001",
            passwordHash: passwordHasher.HashPassword("admin123"),
            sector: "Operaciones y Despacho");

        var lucas = new Repartidor(
            nombre: "Lucas Gómez",
            email: "lucas@rutasegura.com",
            telefono: "11-4000-0002",
            passwordHash: passwordHasher.HashPassword("repartidor123"),
            vehiculo: "Moto Honda 150cc",
            disponible: true,
            capacidadMaxima: 5);

        var martin = new Repartidor(
            nombre: "Martín Rodríguez",
            email: "martin@rutasegura.com",
            telefono: "11-4000-0003",
            passwordHash: passwordHasher.HashPassword("repartidor123"),
            vehiculo: "Furgón Renault Kangoo",
            disponible: true,
            capacidadMaxima: 10);

        var carlos = new Repartidor(
            nombre: "Carlos Pérez",
            email: "carlos@rutasegura.com",
            telefono: "11-4000-0004",
            passwordHash: passwordHasher.HashPassword("repartidor123"),
            vehiculo: "Auto Chevrolet Corsa",
            disponible: true,
            capacidadMaxima: 6);

        await context.Usuarios.AddRangeAsync(encargado, lucas, martin, carlos);
        await context.SaveChangesAsync();

        // 2. Clientes para pruebas
        var clientes = new List<Cliente>
        {
            new("Farmacia Central", "11-5555-0101", "Av. San Martín 1234", "Esquina 9 de Julio"),
            new("Distribuidora Norte", "11-5555-0102", "Calle Mitre 456", "Timbre 2B"),
            new("Restaurante Don Pepe", "11-5555-0103", "Belgrano 789", "Portón Negro"),
            new("Kiosco La Estación", "11-5555-0104", "Av. Hipólito Yrigoyen 321", "Frente a la estación"),
            new("Panadería La Nueva", "11-5555-0105", "Av. Calchaquí 2210", "Local a la calle"),
            new("Ferretería El Tornillo", "11-5555-0106", "Av. 12 de Octubre 850", "Galpón azul"),
            new("Librería Mitre", "11-5555-0107", "Peatonal Rivadavia 142", "Piso 1"),
            new("Supermercado Calchaquí", "11-5555-0108", "Av. Calchaquí 3500", "Entrada proveedores"),
            new("Heladería Freddo", "11-5555-0109", "Av. San Martín 890", "Esquina Alvear"),
            new("Vinoteca Los Andes", "11-5555-0110", "Lavalle 430", "Timbre blanco")
        };

        await context.Clientes.AddRangeAsync(clientes);
        await context.SaveChangesAsync();
    }
}
