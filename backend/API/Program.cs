using API.Middleware;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Autenticación con cookies nativas HttpOnly (RF01)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RutaSegura.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Database Context (SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=RutaSeguraDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Inyección de dependencias - Servicios técnicos
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

// Inyección de dependencias - Persistencia
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IRepartidorRepository, RepartidorRepository>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();

// Inyección de dependencias - Casos de uso (Application)
// Usuarios / Autenticación
builder.Services.AddScoped<Application.UseCases.Usuarios.LoginUsuario.LoginUsuarioHandler>();

// Repartidores (RF02, RF09, RF10, RF11)
builder.Services.AddScoped<Application.UseCases.Repartidores.CrearRepartidor.CrearRepartidorHandler>();
builder.Services.AddScoped<Application.UseCases.Repartidores.ActualizarRepartidor.ActualizarRepartidorHandler>();
builder.Services.AddScoped<Application.UseCases.Repartidores.CambiarDisponibilidad.CambiarDisponibilidadHandler>();
builder.Services.AddScoped<Application.UseCases.Repartidores.ObtenerRepartidores.ObtenerRepartidoresHandler>();
builder.Services.AddScoped<Application.UseCases.Repartidores.ObtenerRepartidorPorId.ObtenerRepartidorPorIdHandler>();
builder.Services.AddScoped<Application.UseCases.Repartidores.AsignarPedido.AsignarPedidoHandler>();
builder.Services.AddScoped<Application.UseCases.Repartidores.ConsultarCargaTrabajo.ConsultarCargaTrabajoHandler>();

// Clientes (RF03, RF04, RF05)
builder.Services.AddScoped<Application.UseCases.Clientes.BuscarClientes.BuscarClientesHandler>();
builder.Services.AddScoped<Application.UseCases.Clientes.CrearCliente.CrearClienteHandler>();
builder.Services.AddScoped<Application.UseCases.Clientes.ActualizarCliente.ActualizarClienteHandler>();
builder.Services.AddScoped<Application.UseCases.Clientes.EliminarCliente.EliminarClienteHandler>();
builder.Services.AddScoped<Application.UseCases.Clientes.ObtenerClientePorId.ObtenerClientePorIdHandler>();

// Pedidos (RF06, RF07, RF08)
builder.Services.AddScoped<Application.UseCases.Pedidos.CrearPedido.CrearPedidoHandler>();
builder.Services.AddScoped<Application.UseCases.Pedidos.ActualizarPedido.ActualizarPedidoHandler>();
builder.Services.AddScoped<Application.UseCases.Pedidos.CancelarPedido.CancelarPedidoHandler>();
builder.Services.AddScoped<Application.UseCases.Pedidos.FiltrarPedidos.FiltrarPedidosHandler>();
builder.Services.AddScoped<Application.UseCases.Pedidos.ObtenerPedidoPorId.ObtenerPedidoPorIdHandler>();

// Seguimiento e Incidencias (RF12, RF13, RF14)
builder.Services.AddScoped<Application.UseCases.Seguimiento.ActualizarEstadoEntrega.ActualizarEstadoEntregaHandler>();
builder.Services.AddScoped<Application.UseCases.Seguimiento.RegistrarIncidencia.RegistrarIncidenciaHandler>();
builder.Services.AddScoped<Application.UseCases.Seguimiento.ConsultarHistorialEntregas.ConsultarHistorialEntregasHandler>();

var app = builder.Build();

// Manejo global de excepciones al inicio del pipeline
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
