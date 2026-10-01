/**
 * Ruta Segura - Cliente API HTTP
 * Sprint 4: Seguimiento e Incidencias (Vertical Slice)
 * IMPORTANTE: Mandatorio el uso de `credentials: 'include'` en todas las peticiones
 * para gestionar automáticamente la cookie nativa HttpOnly (`RutaSegura.Session`).
 */

// Base URL configurable: detecta localhost:5086 (backend .NET) o almacena override en localStorage
const DEFAULT_API_BASE = 'http://localhost:5086';
export const API_BASE = localStorage.getItem('rutasegura_api_url') || DEFAULT_API_BASE;

export function setApiBaseUrl(url) {
    if (url) {
        localStorage.setItem('rutasegura_api_url', url.replace(/\/+$/, ''));
        window.location.reload();
    }
}

/**
 * Wrapper centralizado de fetch con 'credentials: include' y manejo estandarizado de errores
 */
async function request(endpoint, options = {}) {
    const url = `${API_BASE}${endpoint}`;
    const headers = {
        'Content-Type': 'application/json',
        ...(options.headers || {})
    };

    const config = {
        ...options,
        headers,
        // MANDATORIO: Asegura el envío y recepción de la cookie HttpOnly 'RutaSegura.Session'
        credentials: 'include'
    };

    try {
        const response = await fetch(url, config);

        if (response.status === 204) {
            return null;
        }

        const data = await response.json().catch(() => null);

        if (!response.ok) {
            const errorMsg = data?.message || data?.title || data?.detail || `Error HTTP ${response.status}`;
            const error = new Error(errorMsg);
            error.status = response.status;
            error.data = data;
            throw error;
        }

        return data;
    } catch (err) {
        if (err.name === 'TypeError' && err.message.includes('fetch')) {
            throw new Error(`No se pudo conectar con el servidor en ${API_BASE}. Verifique que el Backend esté ejecutándose.`);
        }
        throw err;
    }
}

export const api = {
    // Autenticación (RF01)
    auth: {
        login: (email, password) => request('/api/auth/login', {
            method: 'POST',
            body: JSON.stringify({ email, password })
        }),
        logout: () => request('/api/auth/logout', {
            method: 'POST'
        }),
        getMe: () => request('/api/auth/me')
    },

    // Pedidos (RF06, RF07, RF08)
    pedidos: {
        getAll: (params = {}) => {
            const query = new URLSearchParams();
            if (params.estado) query.append('estado', params.estado);
            if (params.repartidorId) query.append('repartidorId', params.repartidorId);
            const qs = query.toString();
            return request(`/api/pedidos${qs ? '?' + qs : ''}`);
        },
        getById: (id) => request(`/api/pedidos/${id}`),
        cancelar: (id) => request(`/api/pedidos/${id}`, { method: 'DELETE' })
    },

    // Repartidores (RF09, RF10, RF11)
    repartidores: {
        getAll: () => request('/api/repartidores'),
        asignar: (repartidorId, pedidoId) => request(`/api/repartidores/${repartidorId}/asignar`, {
            method: 'POST',
            body: JSON.stringify({ pedidoId })
        }),
        getCarga: (id) => request(`/api/repartidores/${id}/carga`)
    },

    // Seguimiento e Incidencias (RF12, RF13, RF14)
    seguimiento: {
        // RF12: Actualizar estado de entrega (EnCamino, Entregado, Cancelado)
        actualizarEstado: (pedidoId, nuevoEstado) => request(`/api/seguimiento/${pedidoId}/estado`, {
            method: 'PATCH',
            body: JSON.stringify({ nuevoEstado })
        }),

        // RF13: Registrar incidencia sobre un pedido
        registrarIncidencia: (pedidoId, tipo, descripcion) => request(`/api/seguimiento/${pedidoId}/incidencias`, {
            method: 'POST',
            body: JSON.stringify({ tipo, descripcion })
        }),

        // RF14: Consulta cronológica y trazabilidad de eventos
        getHistorial: (filtros = {}) => {
            const query = new URLSearchParams();
            if (filtros.soloEntregados !== undefined) query.append('soloEntregados', filtros.soloEntregados);
            if (filtros.soloConIncidencias !== undefined) query.append('soloConIncidencias', filtros.soloConIncidencias);
            if (filtros.repartidorId) query.append('repartidorId', filtros.repartidorId);
            const qs = query.toString();
            return request(`/api/seguimiento/historial${qs ? '?' + qs : ''}`);
        },

        // RF14 (LINQ e): Conteo agrupado de incidencias por tipo (Dictionary<string, int>)
        getConteoIncidencias: () => request('/api/seguimiento/conteo-incidencias'),

        // RF14 (LINQ e): Entregas concretadas por repartidor (Dictionary<string, int>)
        getEntregasPorRepartidor: () => request('/api/seguimiento/entregas-por-repartidor')
    }
};
