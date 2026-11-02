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
docker compose up -d
dotnet run --project backend/API
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

Representa el flujo principal del ciclo de vida del pedido (**Asignación**, **Despacho / Entrega** y **Resolución con Incidencia**) interactuando entre la UI, la API, las entidades del Dominio y la persistencia:

```mermaid
sequenceDiagram
    actor Usuario as Usuario (Encargado / Repartidor)
    participant UI as Frontend (Web App)
    participant API as Backend (API)
    participant Dominio as Pedido (Dominio)
    participant BD as Base de Datos

    %% 1. Asignar Pedido
    rect rgb(240, 245, 255)
    Note over Usuario, BD: 1. Asignar Pedido al Repartidor
    Usuario->>UI: Selecciona pedido y asigna repartidor
    UI->>API: POST /api/repartidores/asignar-pedido
    API->>Dominio: AsignarRepartidor(repartidor)
    Dominio-->>API: Estado = Asignado
    API->>BD: Guardar cambios
    API-->>UI: 200 OK (Asignado)
    end

    %% 2. Despacho y Entrega
    rect rgb(240, 255, 245)
    Note over Usuario, BD: 2. Despacho y Entrega Exitosa
    Usuario->>UI: Marca pedido "En Camino"
    UI->>API: PUT /api/seguimiento/estado (EnCamino)
    API->>Dominio: MarcarEnCamino()
    Dominio-->>API: Estado = EnCamino
    API->>BD: Guardar cambios

    Usuario->>UI: Confirma entrega en destino
    UI->>API: PUT /api/seguimiento/estado (Entregado)
    API->>Dominio: RegistrarEntrega()
    Dominio-->>API: Estado = Entregado
    API->>BD: Guardar cambios
    API-->>UI: 200 OK (Entrega completada)
    end

    %% 3. Caso Alternativo: Incidencia
    rect rgb(255, 245, 245)
    Note over Usuario, BD: 3. Flujo Alternativo: Registro de Incidencia
    Usuario->>UI: Reporta inconveniente en calle
    UI->>API: POST /api/seguimiento/incidencias
    API->>Dominio: RegistrarIncidencia(incidencia)
    Dominio-->>API: Estado = ConInconveniente
    API->>BD: Guardar cambios
    API-->>UI: 201 Created (Alerta registrada)
    end
```

### Descripción del Flujo

1. **Asignación del Pedido**: El Encargado selecciona un pedido y un repartidor disponible. La API invoca `AsignarRepartidor(repartidor)` sobre el `Pedido`, actualizando su estado a `Asignado` y guardando en la base de datos.
2. **Despacho y Entrega**: El Repartidor inicia el viaje (`MarcarEnCamino()`) y al llegar a destino confirma la entrega (`RegistrarEntrega()`), alcanzando el estado terminal `Entregado`.
3. **Flujo de Incidencia**: Si surge un inconveniente (cliente ausente, dirección errónea), se reporta a la API, la cual crea la `Incidencia` y ejecuta `RegistrarIncidencia(incidencia)`, dejando el pedido en estado `ConInconveniente` para su seguimiento.