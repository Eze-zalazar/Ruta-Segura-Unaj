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

        // 2. Clientes
        var cliente1 = new Cliente("Farmacia Central", "11-5555-0101", "Av. San Martín 1234", "Esquina 9 de Julio");
        var cliente2 = new Cliente("Distribuidora Norte", "11-5555-0102", "Calle Mitre 456", "Timbre 2B");
        var cliente3 = new Cliente("Restaurante Don Pepe", "11-5555-0103", "Belgrano 789", "Portón Negro");
        var cliente4 = new Cliente("Kiosco La Estación", "11-5555-0104", "Av. Hipólito Yrigoyen 321", "Frente a la estación");

        await context.Clientes.AddRangeAsync(cliente1, cliente2, cliente3, cliente4);
        await context.SaveChangesAsync();

        // 3. Pedidos con distintos estados y prioridades
        var pedido1 = new Pedido(
            clienteId: cliente1.Id,
            fechaPactada: DateTime.UtcNow.AddHours(2),
            prioridad: PrioridadPedido.Urgente,
            descripcion: "Caja de Medicamentos e Insumos Críticos",
            observaciones: "Entregar urgente en mesa de entrada");
        lucas.AsignarPedido(pedido1);
        pedido1.MarcarEnCamino();

        var pedido2 = new Pedido(
            clienteId: cliente2.Id,
            fechaPactada: DateTime.UtcNow.AddHours(-2),
            prioridad: PrioridadPedido.Media,
            descripcion: "Pack 24x Bebidas y Snacks",
            observaciones: "Dejar en depósito lateral");
        martin.AsignarPedido(pedido2);
        pedido2.MarcarEnCamino();
        pedido2.RegistrarEntrega();

        var pedido3 = new Pedido(
            clienteId: cliente3.Id,
            fechaPactada: DateTime.UtcNow.AddHours(4),
            prioridad: PrioridadPedido.Alta,
            descripcion: "Caja con cristalería gastronómica",
            observaciones: "Frágil, manipular con cuidado");
        lucas.AsignarPedido(pedido3);

        var pedido4 = new Pedido(
            clienteId: cliente4.Id,
            fechaPactada: DateTime.UtcNow.AddDays(1),
            prioridad: PrioridadPedido.Baja,
            descripcion: "Caja surtida de golosinas",
            observaciones: "Cobro contra entrega pactado");

        var pedido5 = new Pedido(
            clienteId: cliente1.Id,
            fechaPactada: DateTime.UtcNow.AddHours(1),
            prioridad: PrioridadPedido.Urgente,
            descripcion: "Reactivos de laboratorio refrigerados",
            observaciones: "Requiere cadena de frío");
        carlos.AsignarPedido(pedido5);
        pedido5.MarcarEnCamino();

        await context.Pedidos.AddRangeAsync(pedido1, pedido2, pedido3, pedido4, pedido5);
        await context.SaveChangesAsync();

        // 4. Incidencias
        var inc1 = new Incidencia(pedido1.Id, "Demora", "Corte por obras de pavimentación en Av. San Martín.");
        pedido1.RegistrarIncidencia(inc1);

        var inc2 = new Incidencia(pedido5.Id, "Rotura", "Rotura en el sello de precinto exterior durante manipulación.");
        pedido5.RegistrarIncidencia(inc2);

        await context.SaveChangesAsync();
    }
}
