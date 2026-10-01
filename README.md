# Ruta Segura - UNAJ

Sistema web para la gestión, asignación y seguimiento de entregas en pequeños comercios y empresas que cuentan con logística propia.

---

## 📌 ¿Qué problema resuelve y quiénes lo usarían?

Pequeños comercios y empresas que realizan sus propias entregas suelen organizar los pedidos mediante WhatsApp, llamadas o anotaciones manuales. Cuando hay varias entregas en curso, resulta complejo saber con precisión qué pedido tiene asignado cada repartidor, cuáles ya fueron entregados y cuáles tuvieron inconvenientes.

### Usuarios del sistema:
* **Encargado / Coordinador de entregas**: Registra clientes y pedidos, asigna las entregas a los repartidores y monitorea los estados e incidencias.
* **Repartidores**: Consultan sus pedidos asignados, actualizan su disponibilidad y el estado de la entrega, y reportan inconvenientes ocurridos en la calle.

---

## 🎯 Alcance del MVP (Mínimo Producto Viable)

### ✅ Funcionalidades Incluidas:
* **Gestión de Clientes**: Registro y actualización de datos de contacto y direcciones con referencias.
* **Gestión de Pedidos**: Alta de pedidos con fecha de creación, fecha pactada, descripción y nivel de prioridad (`Baja`, `Media`, `Alta`, `Urgente`).
* **Asignación y Seguimiento**: Asignación de pedidos a repartidores disponibles y transición de estados (`Pendiente`, `Asignado`, `EnCamino`, `Entregado`, `ConInconveniente`).
* **Registro de Incidencias**: Carga de problemas ocurridos durante el reparto (cliente ausente, dirección errónea, rechazo del pedido, etc.).
* **Consultas y Filtros**: Visualización de pedidos organizados por su estado y ordenados por fecha o prioridad.
* **Control de Acceso**: Autenticación para usuarios autorizados con perfiles diferenciados (`Encargado` y `Repartidor`).

### ❌ Fuera del Alcance (Exclusiones explícitas para el MVP):
* Seguimiento GPS en tiempo real.
* Optimización automática de rutas.
* Integración con mapas interactivos (Google Maps / OpenStreetMap).
* Facturación electrónica y pasarelas de pago.
* Comunicación directa o automatizada con la API de WhatsApp.

---

## 🛠️ Metodología y Tecnologías

* **Metodología**: Scrum, con entregas semanales atadas al calendario de clases y seguimiento mediante tablero en Trello.
* **Lenguaje y Framework**: C# con ASP.NET Core (.NET 9).
* **Persistencia**: Entity Framework Core.
* **Diseño**: Aplicación web responsive.

---

## 📐 Diagrama de Clases UML

```mermaid
classDiagram
    direction TB

    %% Enumeraciones
    class EstadoPedido {
        <<enumeration>>
        Pendiente
        Asignado
        EnCamino
        Entregado
        ConInconveniente
        Cancelado
    }

    class PrioridadPedido {
        <<enumeration>>
        Baja
        Media
        Alta
        Urgente
    }

    %% Jerarquía de Usuarios (Herencia)
    class Usuario {
        <<abstract>>
        -id: int
        -nombre: string
        -email: string
        -telefono: string
        -passwordHash: string
        +actualizarDatosBasicos(nombre: string, tel: string) void
        +actualizarPassword(nuevoPasswordHash: string) void
        +obtenerRol()* string
        +puedeAsignarseEntregas()* bool
    }

    class Encargado {
        -sector: string
        +obtenerRol() string
        +puedeAsignarseEntregas() bool
    }

    class Repartidor {
        -vehiculo: string
        -disponible: bool
        +obtenerRol() string
        +puedeAsignarseEntregas() bool
        +setDisponible(disp: bool) void
        +actualizarVehiculo(vehiculo: string) void
        +agregarPedido(p: Pedido) void
    }

    %% Clases del Dominio
    class Cliente {
        -id: int
        -nombre: string
        -telefono: string
        -direccion: string
        -referencia: string
        +actualizarContacto(tel: string, dir: string, ref: string) void
        +actualizarNombre(nombre: string) void
        +agregarPedido(p: Pedido) void
    }

    class Pedido {
        -id: int
        -fechaCreacion: DateTime
        -fechaPactada: DateTime
        -prioridad: PrioridadPedido
        -estado: EstadoPedido
        -descripcion: string
        -observaciones: string
        +asignarRepartidor(r: Repartidor) void
        +cambiarEstado(nuevoEstado: EstadoPedido) void
        +actualizarDatos(descripcion: string, fecha: DateTime, prioridad: PrioridadPedido, obs: string) void
        +marcarEnCamino() void
        +registrarEntrega() void
        +cancelar() void
        +registrarIncidencia(inc: Incidencia) void
    }

    class Incidencia {
        -id: int
        -fechaHora: DateTime
        -tipo: string
        -descripcion: string
        -resuelta: bool
        +marcarResuelta() void
    }

    %% Relaciones
    Usuario <|-- Encargado
    Usuario <|-- Repartidor

    Pedido "0..*" --> "1" Cliente : para
    Pedido "0..*" --> "0..1" Repartidor : asignado a
    Pedido "1" *-- "0..*" Incidencia : contiene

    Pedido ..> EstadoPedido
    Pedido ..> PrioridadPedido
```

---

### Descripción del Diseño UML

1. **Herencia / Generalización (`Usuario <|-- Encargado`, `Usuario <|-- Repartidor`)**:
   `Usuario` es una clase abstracta que provee la lógica común de identidad y credenciales. `Encargado` y `Repartidor` heredan de ella y agregan sus atributos particulares (`sector`, `vehiculo`, disponibilidad), resolviendo polimórficamente la regla de negocio `puedeAsignarseEntregas()`.
2. **Separación de Responsabilidades**:
   La autenticación y orquestación de sesiones reside en la capa de Aplicación (`LoginUsuarioHandler` / `IPasswordHasher`), manteniendo la entidad `Usuario` pura sin acoplamiento a servicios técnicos.
3. **Composición (`Pedido "1" *-- "0..*" Incidencia`)**:
   Las incidencias tienen una relación todo-parte fuerte con el pedido; solo existen ligadas a un pedido determinado y no tienen sentido de forma huérfana.
4. **Asociación**:
   * `Pedido "0..*" --> "1" Cliente`: Todo pedido está asociado obligatoriamente a un único cliente.
   * `Pedido "0..*" --> "0..1" Repartidor`: Un pedido puede estar sin asignar (`0`) o asignado a un repartidor (`1`). A su vez, un repartidor puede tener varios pedidos a su cargo (`*`).
5. **Dependencia**:
   `Pedido` depende de las enumeraciones `EstadoPedido` y `PrioridadPedido` para tipificar de forma consistente su ciclo de vida y urgencia.

---

## 🔄 Diagrama de Secuencia: Autenticación y Sesiones (RF01)

Flujo completo de login e inicio de sesión resuelto de manera directa con cookies nativas `HttpOnly` (`RutaSegura.Session`):

```mermaid
sequenceDiagram
    autonumber
    actor U as Usuario (Cliente Web)
    participant C as AuthController
    participant H as LoginUsuarioHandler
    participant R as IUsuarioRepository
    participant P as IPasswordHasher
    participant S as HttpContext (Cookie Auth)

    U->>C: POST /api/auth/login { email, password }
    C->>H: HandleAsync(LoginUsuarioCommand)
    H->>H: Sanitizar inputs (.Trim().ToLowerInvariant())
    H->>R: GetByEmailAsync(emailNormalizado)
    R-->>H: Usuario (Encargado / Repartidor)
    H->>P: VerifyPassword(password, usuario.PasswordHash)
    P-->>H: true (PBKDF2 / Fallback Legacy)
    H-->>C: UsuarioDto { id, nombre, email, rol }
    C->>S: SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, ClaimsPrincipal)
    Note over C,S: Emite cookie segura "RutaSegura.Session" (HttpOnly, SameSite=Lax, Secure)
    C-->>U: 200 OK + Set-Cookie ("RutaSegura.Session") + UsuarioDto
```

---

## 🔄 Diagrama de Secuencia: Asignación de Pedido a Repartidor (RF11)

Flujo de asignación con validaciones de invariantes de dominio (disponibilidad y capacidad máxima operativa):

```mermaid
sequenceDiagram
    autonumber
    actor E as Encargado (Autenticado)
    participant C as RepartidoresController
    participant H as AsignarPedidoHandler
    participant RR as IRepartidorRepository
    participant PR as IPedidoRepository
    participant R as Repartidor (Domain)
    participant P as Pedido (Domain)
    participant U as IUnitOfWork

    E->>C: POST /api/repartidores/{id}/pedidos/{pedidoId}
    Note over E,C: Protegido con [Authorize] (Cookie HttpOnly)
    C->>H: HandleAsync(AsignarPedidoCommand)
    H->>RR: GetByIdAsync(repartidorId)
    RR-->>H: repartidor
    H->>PR: GetByIdAsync(pedidoId)
    PR-->>H: pedido
    H->>R: AsignarPedido(pedido)
    activate R
    Note over R: 1. Valida Disponible == true
    Note over R: 2. Valida Pedido no entregado/cancelado
    Note over R: 3. Valida CargaActiva < CapacidadMaxima (5)
    R->>P: AsignarRepartidor(this)
    activate P
    Note over P: Estado = Asignado, RepartidorId = id
    deactivate P
    R->>R: _pedidos.Add(pedido)
    deactivate R
    H->>RR: Update(repartidor)
    H->>PR: Update(pedido)
    H->>U: SaveChangesAsync()
    U-->>H: OK
    H-->>C: void
    C-->>E: 204 NoContent
```

---

## 🔄 Diagrama de Secuencia: Transición de Estado y Registro de Incidencias (RF12 / RF13)

Flujo operativo del ciclo de seguimiento y gestión de incidencias con protección de invariantes de dominio:

```mermaid
sequenceDiagram
    autonumber
    actor O as Operador / Repartidor (Web UI)
    participant SC as SeguimientoController
    participant AEH as ActualizarEstadoEntregaHandler
    participant RIH as RegistrarIncidenciaHandler
    participant PR as IPedidoRepository
    participant P as Pedido (Domain)
    participant I as Incidencia (Domain)
    participant U as IUnitOfWork

    rect rgb(240, 245, 255)
    note over O,U: RF12: Transición de Estado de Entrega (EnCamino / Entregado)
    O->>SC: PATCH /api/seguimiento/{id}/estado { nuevoEstado: "EnCamino" }
    Note over O,SC: credentials: 'include' (Cookie HttpOnly)
    SC->>AEH: HandleAsync(ActualizarEstadoEntregaCommand)
    AEH->>PR: GetByIdAsync(pedidoId)
    PR-->>AEH: pedido
    alt nuevoEstado == EnCamino
        AEH->>P: MarcarEnCamino()
        Note over P: Invariante: Repartidor asignado y no cancelado
    else nuevoEstado == Entregado
        AEH->>P: RegistrarEntrega()
        Note over P: Invariante: Pedido despachado y no cancelado
    end
    AEH->>PR: Update(pedido)
    AEH->>U: SaveChangesAsync()
    U-->>AEH: OK
    AEH-->>SC: Task (completado)
    SC-->>O: 204 NoContent
    end

    rect rgb(255, 245, 245)
    note over O,U: RF13: Registro de Incidencia en Pedido Activo
    O->>SC: POST /api/seguimiento/{id}/incidencias { tipo: "Demora", descripcion: "Corte vial" }
    SC->>RIH: HandleAsync(RegistrarIncidenciaCommand)
    RIH->>PR: GetByIdAsync(pedidoId)
    PR-->>RIH: pedido
    RIH->>I: new Incidencia(pedidoId, tipo, descripcion)
    Note over I: Valida descripción >= 5 caracteres
    RIH->>P: RegistrarIncidencia(incidencia)
    Note over P: Invariante: Pedido no finalizado / _incidencias.Add(incidencia)
    RIH->>PR: Update(pedido)
    RIH->>U: SaveChangesAsync()
    U-->>RIH: OK
    RIH-->>SC: IncidenciaDto { id, tipo, descripcion, fechaHora, resuelta }
    SC-->>O: 201 CreatedAtAction + IncidenciaDto
    end
```

---

## 💻 Frontend Inicial: Vertical Slice (FRONT-01)

Interfaz de usuario ligera construida con HTML5, Modern Vanilla JS y CSS (Design System propio) sin dependencias externas pesadas, ubicada en `frontend/`:
- **Seguridad nativa:** Configuración mandatoria de peticiones HTTP con `credentials: 'include'` para intercambio automático de la cookie HttpOnly `RutaSegura.Session`.
- **Tablero de Seguimiento (RF12 / RF13):** Visualización de pedidos, KPIs dinámicos, filtros por estado, actualización de transiciones operativas y registro rápido de incidencias.
- **Historial y Trazabilidad (RF14):** Línea de tiempo cronológica (`OrderByDescending`), proyección de última incidencia (`FirstOrDefault`), y paneles visuales para los diccionarios clave-valor (`Dictionary<string, int>`) de conteo por tipo de incidencia y entregas por repartidor.