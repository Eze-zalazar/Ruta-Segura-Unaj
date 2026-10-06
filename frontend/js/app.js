/**
 * Ruta Segura - Sistema de Gestión Logística & Seguimiento
 * Sprint 4: Lógica de Aplicación Frontend (Vertical Slice)
 */

import { api } from './api.js';

// Estado global de la aplicación
const state = {
    currentUser: null,
    pedidos: [],
    repartidores: [],
    historial: [],
    filtroActual: 'todos',
    filtroHistorial: 'todos'
};

// ====================================================================
// UTILIDADES: TOASTS Y MODALES
// ====================================================================
function showToast(message, type = 'info') {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    
    const icon = type === 'success' ? '✅' : type === 'error' ? '❌' : 'ℹ️';
    toast.innerHTML = `<span>${icon}</span> <span>${message}</span>`;
    
    container.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(100%)';
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}

function openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.classList.add('active');
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.classList.remove('active');
}

// Configurar cierre de modales
document.querySelectorAll('[data-close]').forEach(btn => {
    btn.addEventListener('click', () => {
        const modalId = btn.getAttribute('data-close');
        closeModal(modalId);
    });
});

window.addEventListener('click', (e) => {
    if (e.target.classList.contains('modal-overlay')) {
        e.target.classList.remove('active');
    }
});

// ====================================================================
// AUTENTICACIÓN (RF01 - Native HttpOnly Session)
// ====================================================================
async function checkAuthSession() {
    try {
        const user = await api.auth.getMe();
        if (user && user.email) {
            setLoggedUser(user);
        } else {
            setLoggedUser(null);
        }
    } catch {
        setLoggedUser(null);
    }
}

function setLoggedUser(user) {
    state.currentUser = user;
    const userContainer = document.getElementById('user-info-container');
    const btnLogin = document.getElementById('btn-login-trigger');
    const btnLogout = document.getElementById('btn-logout');

    if (user) {
        userContainer.style.display = 'block';
        document.getElementById('nav-user-name').textContent = user.nombre || user.email;
        document.getElementById('nav-user-role').textContent = user.rol || 'Operador';
        document.getElementById('nav-user-avatar').textContent = (user.nombre || user.email).charAt(0).toUpperCase();
        btnLogin.style.display = 'none';
        btnLogout.style.display = 'inline-flex';
    } else {
        userContainer.style.display = 'none';
        btnLogin.style.display = 'inline-flex';
        btnLogout.style.display = 'none';
    }
}

document.getElementById('btn-login-trigger').addEventListener('click', () => {
    openModal('modal-login');
});

document.getElementById('btn-fill-demo').addEventListener('click', () => {
    document.getElementById('login-email').value = 'admin@rutasegura.com';
    document.getElementById('login-password').value = 'admin123';
});

document.getElementById('form-login').addEventListener('submit', async (e) => {
    e.preventDefault();
    const email = document.getElementById('login-email').value.trim();
    const password = document.getElementById('login-password').value;
    const btn = document.getElementById('btn-submit-login');

    try {
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner"></span> Ingresando...';
        const user = await api.auth.login(email, password);
        setLoggedUser(user);
        closeModal('modal-login');
        showToast(`Bienvenido/a, ${user.nombre || user.email}`, 'success');
        await loadInitialData();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Ingresar';
    }
});

document.getElementById('btn-logout').addEventListener('click', async () => {
    try {
        await api.auth.logout();
        setLoggedUser(null);
        showToast('Sesión cerrada correctamente', 'info');
        loadInitialData();
    } catch (err) {
        showToast(err.message, 'error');
    }
});

// ====================================================================
// CARGA DE DATOS & KPIS
// ====================================================================
async function loadInitialData() {
    await Promise.allSettled([
        loadPedidos(),
        loadRepartidores(),
        loadHistorial(),
        loadMetricasLinq()
    ]);
}

async function loadPedidos() {
    try {
        state.pedidos = await api.pedidos.getAll();
        updateKpis();
        renderPedidos();
    } catch (err) {
        renderEmptyState(err.message);
    }
}

async function loadRepartidores() {
    try {
        state.repartidores = await api.repartidores.getAll();
        populateRepartidoresSelect();
    } catch (err) {
        console.warn('No se pudieron cargar repartidores:', err.message);
    }
}

function updateKpis() {
    const total = state.pedidos.length;
    const enCamino = state.pedidos.filter(p => p.estado === 'EnCamino' || p.estado === 3).length;
    const entregados = state.pedidos.filter(p => p.estado === 'Entregado' || p.estado === 4).length;
    const incidencias = state.pedidos.filter(p => (p.incidencias && p.incidencias.length > 0) || p.estado === 'ConInconveniente' || p.estado === 5).length;

    document.getElementById('kpi-total').textContent = total;
    document.getElementById('kpi-en-camino').textContent = enCamino;
    document.getElementById('kpi-entregados').textContent = entregados;
    document.getElementById('kpi-incidencias').textContent = incidencias;
}

// ====================================================================
// RENDERIZADO DEL TABLERO DE PEDIDOS (RF12 / RF13 / RF11)
// ====================================================================
function renderPedidos() {
    const tbody = document.getElementById('pedidos-table-body');
    tbody.innerHTML = '';

    let pedidosFiltrados = state.pedidos;
    if (state.filtroActual !== 'todos') {
        pedidosFiltrados = state.pedidos.filter(p => {
            const estadoStr = getEstadoString(p.estado);
            return estadoStr.toLowerCase() === state.filtroActual.toLowerCase();
        });
    }

    if (pedidosFiltrados.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7" class="empty-state">
                    <div class="empty-icon">📭</div>
                    <p>No se encontraron pedidos con el filtro "${state.filtroActual}".</p>
                </td>
            </tr>
        `;
        return;
    }

    pedidosFiltrados.forEach(p => {
        const tr = document.createElement('tr');
        const estadoStr = getEstadoString(p.estado);
        const prioridadStr = getPrioridadString(p.prioridad);
        const repartidorNombre = p.repartidorNombre || (p.repartidorId ? `Repartidor #${p.repartidorId}` : null);
        const cantIncidencias = p.incidencias ? p.incidencias.length : 0;

        tr.innerHTML = `
            <td><strong style="color:var(--primary)">#${p.id}</strong></td>
            <td>
                <div><strong>${escapeHtml(p.descripcion)}</strong></div>
                ${p.clienteNombre ? `<div style="font-size:0.75rem; color:var(--text-muted)">Cliente: ${escapeHtml(p.clienteNombre)}</div>` : ''}
                ${cantIncidencias > 0 ? `<div class="incident-indicator">⚠️ ${cantIncidencias} incidencia(s)</div>` : ''}
            </td>
            <td>${formatDate(p.fechaPactada)}</td>
            <td><span class="priority-${prioridadStr.toLowerCase()}">${prioridadStr}</span></td>
            <td>
                ${repartidorNombre 
                    ? `<span style="color:var(--text-main); font-weight:500;">👤 ${escapeHtml(repartidorNombre)}</span>` 
                    : `<button class="btn btn-secondary btn-sm btn-asignar" data-id="${p.id}">+ Asignar</button>`
                }
            </td>
            <td>
                <span class="badge badge-${estadoStr.toLowerCase()}">
                    <span class="badge-dot"></span>
                    ${estadoStr}
                </span>
            </td>
            <td>
                <div class="table-actions">
                    ${(estadoStr !== 'Entregado' && estadoStr !== 'Cancelado') ? `
                        <button class="btn btn-secondary btn-sm btn-cambiar-estado" data-id="${p.id}" data-estado="${estadoStr}">
                            🚚 Estado
                        </button>
                        <button class="btn btn-danger btn-sm btn-cargar-incidencia" data-id="${p.id}">
                            ⚠️ Incidencia
                        </button>
                    ` : `
                        <span style="font-size:0.75rem; color:var(--text-subtle);">Finalizado</span>
                    `}
                </div>
            </td>
        `;
        tbody.appendChild(tr);
    });

    // Vincular botones de acción operativa
    tbody.querySelectorAll('.btn-cambiar-estado').forEach(btn => {
        btn.addEventListener('click', () => {
            const pedidoId = btn.getAttribute('data-id');
            const estadoActual = btn.getAttribute('data-estado');
            abrirModalCambiarEstado(pedidoId, estadoActual);
        });
    });

    tbody.querySelectorAll('.btn-cargar-incidencia').forEach(btn => {
        btn.addEventListener('click', () => {
            const pedidoId = btn.getAttribute('data-id');
            abrirModalIncidencia(pedidoId);
        });
    });

    tbody.querySelectorAll('.btn-asignar').forEach(btn => {
        btn.addEventListener('click', () => {
            const pedidoId = btn.getAttribute('data-id');
            abrirModalAsignar(pedidoId);
        });
    });
}

function renderEmptyState(message) {
    const tbody = document.getElementById('pedidos-table-body');
    tbody.innerHTML = `
        <tr>
            <td colspan="7" class="empty-state">
                <div class="empty-icon">⚠️</div>
                <p>${escapeHtml(message)}</p>
                <button class="btn btn-primary btn-sm" id="btn-reintentar" style="margin-top:1rem;">Reintentar</button>
            </td>
        </tr>
    `;
    const btn = document.getElementById('btn-reintentar');
    if (btn) btn.addEventListener('click', loadInitialData);
}

// Filtros de estado en tablero
document.querySelectorAll('.filter-chip[data-filter]').forEach(chip => {
    chip.addEventListener('click', () => {
        document.querySelectorAll('.filter-chip[data-filter]').forEach(c => c.classList.remove('active'));
        chip.classList.add('active');
        state.filtroActual = chip.getAttribute('data-filter');
        renderPedidos();
    });
});

document.getElementById('btn-refresh').addEventListener('click', async () => {
    showToast('Actualizando datos...', 'info');
    await loadInitialData();
    showToast('Datos actualizados', 'success');
});

// ====================================================================
// MODAL: ACTUALIZAR ESTADO DE ENTREGA (RF12)
// ====================================================================
function abrirModalCambiarEstado(pedidoId, estadoActual) {
    document.getElementById('modal-estado-id').textContent = pedidoId;
    document.getElementById('estado-pedido-id').value = pedidoId;
    document.getElementById('modal-estado-actual').innerHTML = `
        <span class="badge badge-${estadoActual.toLowerCase()}">
            <span class="badge-dot"></span> ${estadoActual}
        </span>
    `;

    const select = document.getElementById('select-nuevo-estado');
    // Si está Asignado, sugerir EnCamino; si está EnCamino, sugerir Entregado
    if (estadoActual === 'Asignado') {
        select.value = 'EnCamino';
    } else if (estadoActual === 'EnCamino') {
        select.value = 'Entregado';
    }

    openModal('modal-estado');
}

document.getElementById('form-actualizar-estado').addEventListener('submit', async (e) => {
    e.preventDefault();
    const pedidoId = parseInt(document.getElementById('estado-pedido-id').value, 10);
    const nuevoEstado = document.getElementById('select-nuevo-estado').value;
    const btn = document.getElementById('btn-guardar-estado');

    try {
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner"></span> Guardando...';
        await api.seguimiento.actualizarEstado(pedidoId, nuevoEstado);
        showToast(`Pedido #${pedidoId} actualizado a ${nuevoEstado}`, 'success');
        closeModal('modal-estado');
        await loadInitialData();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Confirmar Transición';
    }
});

// ====================================================================
// MODAL: REGISTRAR INCIDENCIA (RF13)
// ====================================================================
function abrirModalIncidencia(pedidoId) {
    document.getElementById('modal-incidencia-id').textContent = pedidoId;
    document.getElementById('incidencia-pedido-id').value = pedidoId;
    document.getElementById('input-incidencia-desc').value = '';
    openModal('modal-incidencia');
}

document.getElementById('form-registrar-incidencia').addEventListener('submit', async (e) => {
    e.preventDefault();
    const pedidoId = parseInt(document.getElementById('incidencia-pedido-id').value, 10);
    const tipo = document.getElementById('select-incidencia-tipo').value;
    const descripcion = document.getElementById('input-incidencia-desc').value.trim();
    const btn = document.getElementById('btn-guardar-incidencia');

    if (descripcion.length < 5) {
        showToast('La descripción debe contener al menos 5 caracteres.', 'error');
        return;
    }

    try {
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner"></span> Registrando...';
        await api.seguimiento.registrarIncidencia(pedidoId, tipo, descripcion);
        showToast(`Incidencia registrada exitosamente en Pedido #${pedidoId}`, 'success');
        closeModal('modal-incidencia');
        await loadInitialData();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Registrar Incidencia';
    }
});

// ====================================================================
// MODAL: ASIGNAR REPARTIDOR (RF11)
// ====================================================================
function populateRepartidoresSelect() {
    const select = document.getElementById('select-repartidor');
    select.innerHTML = '<option value="">-- Seleccionar Repartidor --</option>';
    
    state.repartidores.forEach(r => {
        const option = document.createElement('option');
        option.value = r.id;
        option.textContent = `${r.nombre} (${r.vehiculo || 'Sin vehículo'})${r.disponible ? ' - Disponible' : ' - Ocupado'}`;
        if (!r.disponible) option.disabled = true;
        select.appendChild(option);
    });
}

function abrirModalAsignar(pedidoId) {
    document.getElementById('modal-asignar-id').textContent = pedidoId;
    document.getElementById('asignar-pedido-id').value = pedidoId;
    openModal('modal-asignar');
}

document.getElementById('form-asignar-repartidor').addEventListener('submit', async (e) => {
    e.preventDefault();
    const pedidoId = parseInt(document.getElementById('asignar-pedido-id').value, 10);
    const repartidorId = parseInt(document.getElementById('select-repartidor').value, 10);
    const btn = document.getElementById('btn-guardar-asignacion');

    if (!repartidorId) {
        showToast('Debe seleccionar un repartidor.', 'error');
        return;
    }

    try {
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner"></span> Asignando...';
        await api.repartidores.asignar(repartidorId, pedidoId);
        showToast(`Pedido #${pedidoId} asignado correctamente`, 'success');
        closeModal('modal-asignar');
        await loadInitialData();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Asignar Repartidor';
    }
});

// ====================================================================
// HISTORIAL DE ENTREGAS & TRAZABILIDAD (RF14)
// ====================================================================
async function loadHistorial(filtros = {}) {
    try {
        state.historial = await api.seguimiento.getHistorial(filtros);
        renderHistorial();
    } catch (err) {
        console.warn('Error al cargar historial:', err.message);
    }
}

function renderHistorial() {
    const timeline = document.getElementById('historial-timeline');
    timeline.innerHTML = '';

    if (!state.historial || state.historial.length === 0) {
        timeline.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">📜</div>
                <p>No hay eventos registrados en el historial de entregas.</p>
            </div>
        `;
        return;
    }

    state.historial.forEach(item => {
        const div = document.createElement('div');
        div.className = 'timeline-item';
        const estadoStr = getEstadoString(item.estado);

        div.innerHTML = `
            <div class="timeline-dot"></div>
            <div class="timeline-card">
                <div class="timeline-header">
                    <div>
                        <strong style="color:var(--text-main);">Pedido #${item.pedidoId}: ${escapeHtml(item.descripcion)}</strong>
                        ${item.repartidorNombre ? `<span style="font-size:0.75rem; color:var(--text-muted); margin-left:0.5rem;">Repartidor: ${escapeHtml(item.repartidorNombre)}</span>` : ''}
                    </div>
                    <span class="badge badge-${estadoStr.toLowerCase()}">
                        <span class="badge-dot"></span> ${estadoStr}
                    </span>
                </div>

                <div class="timeline-date">Fecha Pactada: ${formatDate(item.fechaPactada)}</div>

                ${item.ultimaIncidencia ? `
                    <div style="margin-top:0.75rem; padding:0.6rem 0.8rem; background:rgba(239, 68, 68, 0.08); border-left:3px solid #EF4444; border-radius:4px;">
                        <div style="font-size:0.75rem; font-weight:700; color:#F87171;">
                            Última Incidencia: [${escapeHtml(item.ultimaIncidencia.tipo)}] - ${formatDate(item.ultimaIncidencia.fechaHora)}
                        </div>
                        <div style="font-size:0.8rem; color:var(--text-muted); margin-top:0.2rem;">
                            ${escapeHtml(item.ultimaIncidencia.descripcion)}
                        </div>
                    </div>
                ` : ''}

                ${item.incidencias && item.incidencias.length > 1 ? `
                    <details style="margin-top:0.5rem; font-size:0.75rem; color:var(--text-muted);">
                        <summary style="cursor:pointer; color:var(--primary);">Ver ${item.incidencias.length} incidencias registradas</summary>
                        <ul style="list-style:none; padding-left:0.5rem; margin-top:0.4rem;">
                            ${item.incidencias.map(i => `
                                <li style="margin-bottom:0.25rem;">
                                    • <strong>[${escapeHtml(i.tipo)}]</strong> ${escapeHtml(i.descripcion)} <em>(${formatDate(i.fechaHora)})</em>
                                </li>
                            `).join('')}
                        </ul>
                    </details>
                ` : ''}
            </div>
        `;
        timeline.appendChild(div);
    });

    // Actualizar resumen rápido
    const deliveredCount = state.historial.filter(h => getEstadoString(h.estado) === 'Entregado').length;
    const incidentsCount = state.historial.filter(h => h.incidencias && h.incidencias.length > 0).length;

    document.getElementById('quick-history-stats').innerHTML = `
        <div style="display:flex; justify-content:space-between; margin-bottom:0.4rem;">
            <span>Entregas completadas:</span>
            <strong style="color:#34D399">${deliveredCount}</strong>
        </div>
        <div style="display:flex; justify-content:space-between;">
            <span>Pedidos con historial de incidencias:</span>
            <strong style="color:#F87171">${incidentsCount}</strong>
        </div>
    `;
}

// Filtros para historial
document.getElementById('btn-hist-todos')?.addEventListener('click', (e) => {
    setHistorialFilterBtn(e.target);
    loadHistorial({});
});

document.getElementById('btn-hist-entregados')?.addEventListener('click', (e) => {
    setHistorialFilterBtn(e.target);
    loadHistorial({ soloEntregados: true });
});

document.getElementById('btn-hist-incidencias')?.addEventListener('click', (e) => {
    setHistorialFilterBtn(e.target);
    loadHistorial({ soloConIncidencias: true });
});

function setHistorialFilterBtn(activeBtn) {
    ['btn-hist-todos', 'btn-hist-entregados', 'btn-hist-incidencias'].forEach(id => {
        document.getElementById(id)?.classList.remove('active');
    });
    activeBtn?.classList.add('active');
}

// ====================================================================
// MÉTRICAS LINQ CLAVE-VALOR (RF14 LINQ e)
// ====================================================================
async function loadMetricasLinq() {
    try {
        const [conteosIncidencias, conteosRepartidores] = await Promise.all([
            api.seguimiento.getConteoIncidencias(),
            api.seguimiento.getEntregasPorRepartidor()
        ]);

        renderConteoList('conteo-incidencias-list', conteosIncidencias, 'tipo de incidencia');
        renderConteoList('conteo-repartidores-list', conteosRepartidores, 'repartidor con entregas');
    } catch (err) {
        console.warn('Error al cargar métricas LINQ:', err.message);
    }
}

function renderConteoList(elementId, dictionary, emptyLabel) {
    const container = document.getElementById(elementId);
    if (!container) return;
    container.innerHTML = '';

    const entries = Object.entries(dictionary || {});
    if (entries.length === 0) {
        container.innerHTML = `<div class="empty-state" style="padding:1rem;">No hay registros de ${emptyLabel}.</div>`;
        return;
    }

    entries.forEach(([key, val]) => {
        const item = document.createElement('div');
        item.className = 'key-value-item';
        item.innerHTML = `
            <span>${escapeHtml(key)}</span>
            <span class="key-value-count">${val}</span>
        `;
        container.appendChild(item);
    });
}

// ====================================================================
// GESTIÓN DE PESTAÑAS (TABS)
// ====================================================================
document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));

        btn.classList.add('active');
        const targetId = btn.getAttribute('data-tab');
        const targetContent = document.getElementById(targetId);
        if (targetContent) targetContent.classList.add('active');

        if (targetId === 'tab-historial') {
            loadHistorial();
        } else if (targetId === 'tab-metricas') {
            loadMetricasLinq();
        }
    });
});

// ====================================================================
// HELPERS DE FORMATEO Y SEGURIDAD
// ====================================================================
function getEstadoString(estado) {
    if (typeof estado === 'string') return estado;
    switch (estado) {
        case 1: return 'Pendiente';
        case 2: return 'Asignado';
        case 3: return 'EnCamino';
        case 4: return 'Entregado';
        case 5: return 'ConInconveniente';
        case 6: return 'Cancelado';
        default: return 'Desconocido';
    }
}

function getPrioridadString(prioridad) {
    if (typeof prioridad === 'string') return prioridad;
    switch (prioridad) {
        case 1: return 'Baja';
        case 2: return 'Media';
        case 3: return 'Alta';
        default: return 'Media';
    }
}

function formatDate(dateStr) {
    if (!dateStr) return '-';
    try {
        const d = new Date(dateStr);
        return d.toLocaleDateString('es-AR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    } catch {
        return dateStr;
    }
}

function escapeHtml(str) {
    if (!str) return '';
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

// Inicialización de la aplicación
document.addEventListener('DOMContentLoaded', async () => {
    await checkAuthSession();
    await loadInitialData();
});
