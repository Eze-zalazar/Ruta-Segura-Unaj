/**
 * Ruta Segura - Cliente API HTTP
 * Sprint 4: Seguimiento e Incidencias (Vertical Slice)
 * IMPORTANTE: Mandatorio el uso de `credentials: 'include'` en todas las peticiones
 * para gestionar automáticamente la cookie nativa HttpOnly (`RutaSegura.Session`).
 */

// Base URL configurable: detecta automáticamente si el frontend corre en 127.0.0.1 o localhost para preservar el contexto de cookies
const defaultHost = (typeof window !== 'undefined' && window.location.hostname === '127.0.0.1') ? '127.0.0.1' : 'localhost';
const DEFAULT_API_BASE = `http://${defaultHost}:5086`;
export const API_BASE = (typeof localStorage !== 'undefined' && localStorage.getItem('rutasegura_api_url')) || DEFAULT_API_BASE;

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
        create: (data) => request('/api/pedidos', {
            method: 'POST',
            body: JSON.stringify(data)
        }),
        update: (id, data) => request(`/api/pedidos/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data)
        }),
        cancelar: (id) => request(`/api/pedidos/${id}/cancelar`, { method: 'POST' })
    },

    // Clientes (RF03, RF04, RF05)
    clientes: {
        getAll: (termino = '') => {
            const qs = termino ? `?termino=${encodeURIComponent(termino)}` : '';
            return request(`/api/clientes${qs}`);
        },
        getById: (id) => request(`/api/clientes/${id}`),
        create: (data) => request('/api/clientes', {
            method: 'POST',
            body: JSON.stringify(data)
        })
    },

    // Repartidores (RF09, RF10, RF11)
    repartidores: {
        getAll: (soloDisponibles) => {
            const qs = soloDisponibles !== undefined ? `?soloDisponibles=${soloDisponibles}` : '';
            return request(`/api/repartidores${qs}`);
        },
        getById: (id) => request(`/api/repartidores/${id}`),
        asignar: (repartidorId, pedidoId) => request(`/api/repartidores/${repartidorId}/pedidos/${pedidoId}`, {
            method: 'POST'
        }),
        getCarga: (params = {}) => {
            const query = new URLSearchParams();
            if (params.soloDisponibles !== undefined) query.append('soloDisponibles', params.soloDisponibles);
            if (params.soloConCapacidad !== undefined) query.append('soloConCapacidad', params.soloConCapacidad);
            const qs = query.toString();
            return request(`/api/repartidores/carga${qs ? '?' + qs : ''}`);
        },
        setDisponibilidad: (id, disponible) => request(`/api/repartidores/${id}/disponibilidad`, {
            method: 'PATCH',
            body: JSON.stringify({ disponible })
        })
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
