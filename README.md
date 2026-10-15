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
* **Lenguaje y Framework**: C# con ASP.NET Core (.NET 8).
* **Base de Datos**: Microsoft SQL Server (Docker).
* **Persistencia**: Entity Framework Core con SQL Server.
* **Diseño**: Aplicación web responsive (Vanilla HTML/CSS/JS).

---

## 🚀 Cómo levantar el proyecto y correr las pruebas

```bash
# 1. Levantar la base de datos (SQL Server en contenedor)
docker compose up -d

# 2. Levantar el backend (aplica migraciones y seed de usuarios automáticamente)
dotnet run --project backend/API

# 3. Correr todas las pruebas unitarias (44 pruebas)
dotnet test backend/RutaSegura.sln
```

> **Datos de acceso de prueba:**
> * **Encargado (Admin):** `admin@rutasegura.com` / `admin123`
> * **Repartidor:** `lucas@rutasegura.com` / `repartidor123`
> * **Frontend:** Abrir `frontend/index.html` directamente en el navegador (o con Live Server).
> * **Swagger API:** `http://localhost:5000/swagger` (o puerto asignado en consola).

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

## 💻 Frontend Inicial: Vertical Slice (FRONT-01)

Interfaz de usuario ligera construida con HTML5, Modern Vanilla JS y CSS (Design System propio) sin dependencias externas pesadas, ubicada en `frontend/`:
- **Seguridad nativa:** Configuración mandatoria de peticiones HTTP con `credentials: 'include'` para intercambio automático de la cookie HttpOnly `RutaSegura.Session`.
- **Tablero de Seguimiento (RF12 / RF13):** Visualización de pedidos, KPIs dinámicos, filtros por estado, actualización de transiciones operativas y registro rápido de incidencias.
- **Historial y Trazabilidad (RF14):** Línea de tiempo cronológica (`OrderByDescending`), proyección de última incidencia (`FirstOrDefault`), y paneles visuales para los diccionarios clave-valor (`Dictionary<string, int>`) de conteo por tipo de incidencia y entregas por repartidor.

## 🔄 Diagrama de Secuencia UML

Representa la interacción entre los actores, las capas del sistema y las entidades del dominio para el flujo principal de **Gestión del Ciclo de Vida del Pedido (Asignación, Despacho y Resolución por Entrega o Incidencia)**.

```mermaid
sequenceDiagram
    autonumber
    actor Encargado
    actor Repartidor
    participant Frontend as Frontend (Web App)
    participant Backend as Backend (API / Casos de Uso)
    participant Pedido as Pedido (Dominio)
    participant Incidencia as Incidencia (Dominio)
    participant BD as Base de Datos

    %% 1. Asignación del Pedido
    Note over Encargado, BD: 1. Asignación del Pedido
    Encargado->>Frontend: Selecciona pedido pendiente y asigna repartidor
    Frontend->>Backend: POST /api/repartidores/asignar-pedido {pedidoId, repartidorId}
    Backend->>BD: Consultar Pedido y Repartidor
    BD-->>Backend: Entidades recuperadas
    Backend->>Pedido: asignarRepartidor(repartidor)
    activate Pedido
    Pedido-->>Backend: Estado actualizado a "Asignado"
    deactivate Pedido
    Backend->>BD: Guardar cambios (SaveChangesAsync)
    BD-->>Backend: Confirmación de persistencia
    Backend-->>Frontend: 200 OK (Asignación confirmada)
    Frontend-->>Encargado: Muestra confirmación en pantalla

    %% 2. Inicio del Recorrido
    Note over Repartidor, BD: 2. Despacho y Puesta en Camino
    Repartidor->>Frontend: Consulta sus entregas asignadas
    Frontend->>Backend: GET /api/pedidos?repartidorId={id}
    Backend->>BD: Consultar pedidos asignados al repartidor
    BD-->>Backend: Lista de pedidos
    Backend-->>Frontend: 200 OK (Listado de pedidos)
    Frontend-->>Repartidor: Muestra lista de entregas pendientes
    Repartidor->>Frontend: Marca pedido como "En Camino"
    Frontend->>Backend: PUT /api/seguimiento/estado {pedidoId, nuevoEstado: EnCamino}
    Backend->>BD: Obtener Pedido por Id
    BD-->>Backend: Pedido
    Backend->>Pedido: cambiarEstado(EnCamino)
    activate Pedido
    Pedido-->>Backend: Estado actualizado a "EnCamino"
    deactivate Pedido
    Backend->>BD: Guardar cambios (SaveChangesAsync)
    BD-->>Backend: Confirmación
    Backend-->>Frontend: 200 OK
    Frontend-->>Repartidor: Estado actualizado a "En Camino"

    %% 3. Finalización del Reparto (Alternativa: Entrega Exitosa vs Incidencia)
    Note over Repartidor, BD: 3. Finalización de la Entrega (Alternativas)
    alt Flujo Exitoso: Entrega Realizada
        Repartidor->>Frontend: Confirma entrega en destino
        Frontend->>Backend: PUT /api/seguimiento/estado {pedidoId, nuevoEstado: Entregado}
        Backend->>BD: Obtener Pedido
        BD-->>Backend: Pedido
        Backend->>Pedido: cambiarEstado(Entregado)
        activate Pedido
        Pedido-->>Backend: Estado actualizado a "Entregado"
        deactivate Pedido
        Backend->>BD: Guardar cambios (SaveChangesAsync)
        BD-->>Backend: Confirmación
        Backend-->>Frontend: 200 OK
        Frontend-->>Repartidor: Entrega registrada exitosamente

    else Flujo Alternativo: Ocurre un Inconveniente (Registro de Incidencia)
        Repartidor->>Frontend: Reporta problema (cliente ausente, dirección errónea, etc.)
        Frontend->>Backend: POST /api/seguimiento/incidencias {pedidoId, tipo, descripcion}
        Backend->>BD: Obtener Pedido
        BD-->>Backend: Pedido
        Backend->>Incidencia: new Incidencia(pedidoId, tipo, descripcion)
        activate Incidencia
        Incidencia-->>Backend: Instancia creada
        deactivate Incidencia
        Backend->>Pedido: registrarIncidencia(incidencia)
        activate Pedido
        Pedido-->>Backend: Estado actualizado a "ConInconveniente"
        deactivate Pedido
        Backend->>BD: Guardar Incidencia y actualizar Pedido
        BD-->>Backend: Confirmación
        Backend-->>Frontend: 201 Created (IncidenciaDTO)
        Frontend-->>Repartidor: Incidencia guardada con éxito
        Frontend-->>Encargado: Alerta visual de incidencia en el panel de monitoreo
    end
```

### Descripción del Flujo de Secuencia

1. **Asignación del Pedido**:
   * El **Encargado** visualiza los pedidos pendientes y selecciona un repartidor disponible.
   * La API procesa la solicitud mediante el caso de uso `AsignarPedido`, invocando el método de dominio `asignarRepartidor(repartidor)` en la entidad `Pedido`. El estado transiciona de `Pendiente` a `Asignado`.
   * Los cambios se persisten en la base de datos a través del `UnitOfWork`.

2. **Puesta en Camino (Despacho)**:
   * El **Repartidor** consulta sus asignaciones desde su dispositivo y, al iniciar el reparto, marca el pedido como "En Camino".
   * El caso de uso `ActualizarEstadoEntrega` invoca `cambiarEstado(EnCamino)`, actualizando el ciclo de vida del pedido a `EnCamino`.

3. **Resolución de la Entrega (Bifurcación `alt`)**:
   * **Camino Feliz (Entrega Exitosa)**: El repartidor llega al destino y confirma la entrega. Se invoca `cambiarEstado(Entregado)`, culminando el proceso de forma exitosa.
   * **Flujo Alternativo (Incidencia)**: Si surge un inconveniente en la calle (domicilio inexistente, cliente ausente, rechazo), el repartidor registra el incidente mediante `RegistrarIncidencia`. Se crea la entidad `Incidencia`, se adjunta al pedido con `registrarIncidencia(incidencia)` y el estado del pedido pasa automáticamente a `ConInconveniente`. Esto dispara una alerta en el panel del encargado para su seguimiento o reprogramación.