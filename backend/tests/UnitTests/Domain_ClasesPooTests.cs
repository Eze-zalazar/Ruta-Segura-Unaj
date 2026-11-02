namespace UnitTests;

using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Xunit;

/// <summary>
/// Suite de pruebas unitarias puras para las Clases del Dominio (Auditoría POO estricta).
/// Garantiza un mínimo de 3 pruebas por clase:
/// (a) Caso feliz / normal
/// (b) Caso borde / límite
/// (c) Caso error / excepción
/// Más la validación de polimorfismo, recorridos de colección y conteo clave-valor.
/// </summary>
public class Domain_ClasesPooTests
{
    // =========================================================================
    // 1. CLIENTE
    // =========================================================================

    [Fact]
    public void Cliente_CasoFeliz_CreaYActualizaCorrectamente()
    {
        // (a) Caso Feliz
        var cliente = new Cliente("Acme Corp", "11223344", "Av. San Martín 123", "Piso 4");

        Assert.Equal("Acme Corp", cliente.Nombre);
        Assert.Equal("11223344", cliente.Telefono);
        Assert.Equal("Av. San Martín 123", cliente.Direccion);
        Assert.Equal("Piso 4", cliente.Referencia);
        Assert.Empty(cliente.Pedidos);

        cliente.ActualizarContacto("99887766", "Calle Mitre 456", null);
        cliente.ActualizarNombre("Acme Renovado");

        Assert.Equal("Acme Renovado", cliente.Nombre);
        Assert.Equal("99887766", cliente.Telefono);
        Assert.Equal("Calle Mitre 456", cliente.Direccion);
        Assert.Null(cliente.Referencia);
    }

    [Fact]
    public void Cliente_CasoBorde_ManejaEspaciosYAgregaPedidosAColeccionProtegida()
    {
        // (b) Caso Borde
        var cliente = new Cliente("  Juan Perez  ", " 112233 ", " Rivadavia 10  ", " ");
        Assert.Equal("Juan Perez", cliente.Nombre);
        Assert.Equal("112233", cliente.Telefono);
        Assert.Equal("Rivadavia 10", cliente.Direccion);
        Assert.Null(cliente.Referencia);

        var pedido = new Pedido(1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Caja chica");
        cliente.AgregarPedido(pedido);

        Assert.Single(cliente.Pedidos);
        Assert.Contains(pedido, cliente.Pedidos);
    }

    [Theory]
    [InlineData("", "1122", "Calle 1")]
    [InlineData("   ", "1122", "Calle 1")]
    [InlineData("Juan", "", "Calle 1")]
    [InlineData("Juan", "1122", "   ")]
    public void Cliente_CasoError_DatosInvalidosLanzanDomainException(string nombre, string telefono, string direccion)
    {
        // (c) Caso Error
        Assert.Throws<DomainException>(() => new Cliente(nombre, telefono, direccion));
    }

    // =========================================================================
    // 2. PEDIDO
    // =========================================================================

    [Fact]
    public void Pedido_CasoFeliz_CicloDeVidaValido()
    {
        // (a) Caso Feliz
        var repartidor = new Repartidor("Carlos", "carlos@correo.com", "1122", "hash", "Moto");
        var pedido = new Pedido(1, DateTime.UtcNow.AddDays(2), PrioridadPedido.Alta, "Documentación");

        Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);

        pedido.AsignarRepartidor(repartidor);
        Assert.Equal(EstadoPedido.Asignado, pedido.Estado);
        Assert.Equal(repartidor.Id, pedido.RepartidorId);

        pedido.MarcarEnCamino();
        Assert.Equal(EstadoPedido.EnCamino, pedido.Estado);

        pedido.RegistrarEntrega();
        Assert.Equal(EstadoPedido.Entregado, pedido.Estado);
    }

    [Fact]
    public void Pedido_CasoBorde_ActualizacionEnPendienteYCancelacionTerminal()
    {
        // (b) Caso Borde
        var fechaInicial = DateTime.UtcNow.AddDays(1);
        var pedido = new Pedido(1, fechaInicial, PrioridadPedido.Baja, "Insumos varios");

        var nuevaFecha = DateTime.UtcNow.AddDays(3);
        pedido.ActualizarDatos("Insumos urgentes de oficina", nuevaFecha, PrioridadPedido.Urgente, "Entregar en puerta");

        Assert.Equal("Insumos urgentes de oficina", pedido.Descripcion);
        Assert.Equal(nuevaFecha, pedido.FechaPactada);
        Assert.Equal(PrioridadPedido.Urgente, pedido.Prioridad);
        Assert.Equal("Entregar en puerta", pedido.Observaciones);

        pedido.Cancelar();
        Assert.Equal(EstadoPedido.Cancelado, pedido.Estado);

        // Cancelado es estado terminal: no se puede volver a cancelar ni modificar
        Assert.Throws<DomainException>(() => pedido.Cancelar());
        Assert.Throws<DomainException>(() => pedido.ActualizarDatos("Otro", nuevaFecha, PrioridadPedido.Baja));
    }

    [Fact]
    public void Pedido_CasoError_TransicionesInvalidasLanzanDomainException()
    {
        // (c) Caso Error
        var pedido = new Pedido(1, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Mercadería");

        // No se puede despachar sin asignar repartidor
        Assert.Throws<DomainException>(() => pedido.MarcarEnCamino());

        // No se puede entregar un pedido no despachado
        Assert.Throws<DomainException>(() => pedido.RegistrarEntrega());

        // Transición forzada arbitraria en CambiarEstado no permitida
        Assert.Throws<DomainException>(() => pedido.CambiarEstado(EstadoPedido.ConInconveniente));
    }

    // =========================================================================
    // 3. REPARTIDOR (POO, RECORRIDOS DE COLECCIONES Y MAP)
    // =========================================================================

    [Fact]
    public void Repartidor_CasoFeliz_AsignacionYCargaTrabajo()
    {
        // (a) Caso Feliz
        var repartidor = new Repartidor("Gonzalo", "gonzalo@correo.com", "112233", "pass", "Auto", disponible: true, capacidadMaxima: 3);
        var p1 = new Pedido(1, 10, DateTime.UtcNow.AddDays(1), PrioridadPedido.Alta, "Paquete 1");

        repartidor.AsignarPedido(p1);

        Assert.Equal(1, repartidor.ObtenerCargaTrabajoActiva());
        Assert.True(repartidor.TieneCapacidadDisponible());
        Assert.True(repartidor.PuedeAsignarseEntregas());
    }

    [Fact]
    public void Repartidor_CasoBorde_AlcanzaCapacidadMaximaExacta()
    {
        // (b) Caso Borde
        var repartidor = new Repartidor("Micaela", "mica@correo.com", "114455", "pass", "Moto", disponible: true, capacidadMaxima: 2);
        var p1 = new Pedido(1, 10, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Entrega 1");
        var p2 = new Pedido(2, 10, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Entrega 2");

        repartidor.AsignarPedido(p1);
        repartidor.AsignarPedido(p2);

        Assert.Equal(2, repartidor.ObtenerCargaTrabajoActiva());
        Assert.False(repartidor.TieneCapacidadDisponible());
        Assert.False(repartidor.PuedeAsignarseEntregas());

        // Liberar una entrega completándola
        p1.MarcarEnCamino();
        p1.RegistrarEntrega();

        Assert.Equal(1, repartidor.ObtenerCargaTrabajoActiva());
        Assert.True(repartidor.TieneCapacidadDisponible());
    }

    [Fact]
    public void Repartidor_CasoError_SuperarCapacidadOInactivoLanzaDomainException()
    {
        // (c) Caso Error
        var repartidor = new Repartidor("Lucas", "lucas@correo.com", "115566", "pass", "Bici", disponible: false, capacidadMaxima: 1);
        var p = new Pedido(1, 10, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Caja");

        // Repartidor no disponible
        Assert.Throws<DomainException>(() => repartidor.AsignarPedido(p));

        repartidor.ActualizarDisponibilidad(true);
        repartidor.AsignarPedido(p);

        var pExtra = new Pedido(2, 10, DateTime.UtcNow.AddDays(1), PrioridadPedido.Media, "Caja 2");
        // Supera capacidad operativa máxima
        Assert.Throws<DomainException>(() => repartidor.AsignarPedido(pExtra));
    }

    [Fact]
    public void Repartidor_CuatroRecorridosColeccion_Y_EstructuraClaveValor()
    {
        // Validación de los 4 recorridos de colecciones, ordenamiento y diccionario
        var rep = new Repartidor("Ana", "ana@correo.com", "112233", "pwd", "Furgón", disponible: true, capacidadMaxima: 10);

        var f1 = new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
        var f2 = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);
        var f3 = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

        var p1 = new Pedido(1, 1, f1, PrioridadPedido.Media, "Pedido Regular 1");
        var p2 = new Pedido(2, 1, f2, PrioridadPedido.Urgente, "Medicamentos Urgentes");
        var p3 = new Pedido(3, 1, f3, PrioridadPedido.Alta, "Herramientas");

        rep.AsignarPedido(p1);
        rep.AsignarPedido(p2);
        rep.AsignarPedido(p3);

        // Simulamos entrega de p1
        p1.MarcarEnCamino();
        p1.RegistrarEntrega();

        // 1. Filtrado que cumple condición (Entregados)
        var entregados = rep.ObtenerPedidosEntregados().ToList();
        Assert.Single(entregados);
        Assert.Equal(1, entregados[0].Id);

        // 2. Filtrado que NO cumple condición (No entregados / Pendientes)
        var noEntregados = rep.ObtenerPedidosNoEntregados().ToList();
        Assert.Equal(2, noEntregados.Count);
        Assert.DoesNotContain(p1, noEntregados);

        // 3. Transformación (Proyección map a string descriptivo)
        var resumen = rep.ObtenerResumenPedidos().ToList();
        Assert.Equal(3, resumen.Count);
        Assert.Contains("Pedido #2: Medicamentos Urgentes", resumen[1]);

        // 4. Búsqueda del primer elemento que cumple condición (Primer urgente no entregado)
        var primerUrgente = rep.BuscarPrimerPedidoUrgente();
        Assert.NotNull(primerUrgente);
        Assert.Equal(2, primerUrgente.Id);

        // 5. Estructura clave-valor (Dictionary): Conteo acumulativo de pedidos por estado
        var conteoMap = rep.ConteoPedidosPorEstado();
        Assert.Equal(1, conteoMap[EstadoPedido.Entregado]);
        Assert.Equal(2, conteoMap[EstadoPedido.Asignado]);

        // 6. Salida ordenada con criterio explícito (Por fecha pactada ascendente)
        var ordenados = rep.ObtenerPedidosOrdenadosPorFechaPactada().ToList();
        Assert.Equal(2, ordenados[0].Id); // f2 (2 Nov)
        Assert.Equal(1, ordenados[1].Id); // f1 (5 Nov)
        Assert.Equal(3, ordenados[2].Id); // f3 (8 Nov)
    }

    // =========================================================================
    // 4. INCIDENCIA
    // =========================================================================

    [Fact]
    public void Incidencia_CasoFeliz_CreaYMarcaResuelta()
    {
        // (a) Caso Feliz
        var incidencia = new Incidencia(100, "DireccionErronea", "No coincide la numeración municipal.");

        Assert.Equal(100, incidencia.PedidoId);
        Assert.Equal("DireccionErronea", incidencia.Tipo);
        Assert.Equal("No coincide la numeración municipal.", incidencia.Descripcion);
        Assert.False(incidencia.Resuelta);

        incidencia.MarcarResuelta();
        Assert.True(incidencia.Resuelta);
    }

    [Fact]
    public void Incidencia_CasoBorde_TrimmingDeDatosYMarcaTemporalDefinida()
    {
        // (b) Caso Borde
        var fechaEsperada = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var incidencia = new Incidencia(5, 100, "  ClienteAusente  ", " Timbre roto ", fechaEsperada, resuelta: false);

        Assert.Equal("ClienteAusente", incidencia.Tipo);
        Assert.Equal("Timbre roto", incidencia.Descripcion);
        Assert.Equal(fechaEsperada, incidencia.FechaHora);
        Assert.False(incidencia.Resuelta);
    }

    [Theory]
    [InlineData(0, "Tipo", "Desc")]
    [InlineData(-1, "Tipo", "Desc")]
    [InlineData(10, "", "Desc")]
    [InlineData(10, "Tipo", "   ")]
    public void Incidencia_CasoError_ParametrosInvalidosLanzanDomainException(int pedidoId, string tipo, string descripcion)
    {
        // (c) Caso Error
        Assert.Throws<DomainException>(() => new Incidencia(pedidoId, tipo, descripcion));
    }

    // =========================================================================
    // 5. ENCARGADO
    // =========================================================================

    [Fact]
    public void Encargado_CasoFeliz_CreacionYRol()
    {
        // (a) Caso Feliz
        var encargado = new Encargado("Martin", "admin@correo.com", "112233", "pwdHash", "Logística Central");

        Assert.Equal("Martin", encargado.Nombre);
        Assert.Equal("Encargado", encargado.ObtenerRol());
        Assert.False(encargado.PuedeAsignarseEntregas());
        Assert.Equal("Logística Central", encargado.Sector);
    }

    [Fact]
    public void Encargado_CasoBorde_ActualizacionSectorConEspacios()
    {
        // (b) Caso Borde
        var encargado = new Encargado("Sofia", "sofia@correo.com", "113344", "pwdHash", "Deposito 1");
        encargado.ActualizarSector("  Distribución Sur  ");

        Assert.Equal("Distribución Sur", encargado.Sector);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Encargado_CasoError_SectorInvalidoLanzaDomainException(string? sectorInvalido)
    {
        // (c) Caso Error
        Assert.Throws<DomainException>(() => new Encargado("Martin", "admin@correo.com", "1122", "hash", sectorInvalido!));
    }

    // =========================================================================
    // 6. USUARIO (POLIMORFISMO Y SUSTITUCIÓN DE LISKOV - LSP)
    // =========================================================================

    [Fact]
    public void Usuario_CasoFeliz_SustitucionPolimorficaLSP()
    {
        // (a) Caso Feliz: Polimorfismo puro tratando instancias a través de la clase base Usuario
        var usuarios = new List<Usuario>
        {
            new Encargado("Jefe Operaciones", "jefe@rutasegura.com", "1122", "pwd", "Operaciones"),
            new Repartidor("Repartidor Activo", "rep@rutasegura.com", "3344", "pwd", "Moto", disponible: true, capacidadMaxima: 3)
        };

        // Comprobación de que no requiere casting manual de tipos
        var roles = usuarios.Select(u => u.ObtenerRol()).ToList();
        Assert.Equal(new[] { "Encargado", "Repartidor" }, roles);

        var aptosParaEntregas = usuarios.Where(u => u.PuedeAsignarseEntregas()).ToList();
        Assert.Single(aptosParaEntregas);
        Assert.Equal("Repartidor Activo", aptosParaEntregas[0].Nombre);
    }

    [Fact]
    public void Usuario_CasoBorde_ModificacionDeCredencialesYDatosBasicos()
    {
        // (b) Caso Borde
        Usuario usuario = new Encargado("Laura", "laura@correo.com", "1199", "pwdVieja", "Mesa de Entrada");
        usuario.ActualizarDatosBasicos("  Laura Gómez  ", " 11998877 ");
        usuario.ActualizarPassword("pwdNuevaHash");

        Assert.Equal("Laura Gómez", usuario.Nombre);
        Assert.Equal("11998877", usuario.Telefono);
        Assert.Equal("pwdNuevaHash", usuario.PasswordHash);
    }

    [Theory]
    [InlineData("Laura", "email-invalido-sin-arroba", "1122", "pwd")]
    [InlineData("", "laura@correo.com", "1122", "pwd")]
    [InlineData("Laura", "laura@correo.com", "", "pwd")]
    [InlineData("Laura", "laura@correo.com", "1122", "")]
    public void Usuario_CasoError_DatosBaseInvalidosLanzanDomainException(string nombre, string email, string tel, string pwd)
    {
        // (c) Caso Error
        Assert.Throws<DomainException>(() => new Encargado(nombre, email, tel, pwd, "Sector"));
    }
}
