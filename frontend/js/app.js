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
    filtroHistorial: 'todos',
    disponible: true
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
            return user;
        } else {
            setLoggedUser(null);
            return null;
        }
    } catch {
        setLoggedUser(null);
        return null;
    }
}

function setLoggedUser(user) {
    state.currentUser = user;
    const userContainer = document.getElementById('user-info-container');
    const btnLogin = document.getElementById('btn-login-trigger');
    const btnLogout = document.getElementById('btn-logout');
    const btnDisponibilidad = document.getElementById('btn-toggle-disponibilidad');
    const btnNuevoPedido = document.getElementById('btn-nuevo-pedido');
    const tabMetricasBtn = document.querySelector('[data-tab="tab-metricas"]');

    if (user) {
        userContainer.style.display = 'block';
        document.getElementById('nav-user-name').textContent = user.nombre || user.email;
        document.getElementById('nav-user-role').textContent = user.rol || 'Operador';
        
        const avatar = document.getElementById('nav-user-avatar');
        avatar.textContent = (user.nombre || user.email).charAt(0).toUpperCase();

        const isRepartidor = (user.rol === 'Repartidor');
        if (isRepartidor) {
            avatar.className = 'user-avatar repartidor-avatar';
            if (btnDisponibilidad) {
                btnDisponibilidad.style.display = 'inline-flex';
                updateDisponibilidadBtn(state.disponible);
            }
            if (btnNuevoPedido) btnNuevoPedido.style.display = 'none';
            if (tabMetricasBtn) tabMetricasBtn.style.display = 'none';

            // Si estaba en la pestaña de métricas, volver a seguimiento
            const activeTab = document.querySelector('.tab-btn.active');
            if (activeTab?.getAttribute('data-tab') === 'tab-metricas') {
                document.querySelector('[data-tab="tab-seguimiento"]')?.click();
            }
        } else {
            avatar.className = 'user-avatar encargado-avatar';
            if (btnDisponibilidad) btnDisponibilidad.style.display = 'none';
            if (btnNuevoPedido) btnNuevoPedido.style.display = 'inline-flex';
            if (tabMetricasBtn) tabMetricasBtn.style.display = 'inline-flex';
        }

        updateRoleContextBanner(user);
        btnLogin.style.display = 'none';
        btnLogout.style.display = 'inline-flex';
    } else {
        userContainer.style.display = 'none';
        if (btnDisponibilidad) btnDisponibilidad.style.display = 'none';
        if (btnNuevoPedido) btnNuevoPedido.style.display = 'none';
        if (tabMetricasBtn) tabMetricasBtn.style.display = 'inline-flex';
        const banner = document.getElementById('role-context-banner');
        if (banner) banner.style.display = 'none';
        btnLogin.style.display = 'inline-flex';
        btnLogout.style.display = 'none';
    }
}

function updateRoleContextBanner(user) {
    const banner = document.getElementById('role-context-banner');
    const icon = document.getElementById('role-context-icon');
    const title = document.getElementById('role-context-title');
    const desc = document.getElementById('role-context-desc');
    if (!banner || !user) return;

    banner.style.display = 'flex';
    if (user.rol === 'Repartidor') {
        banner.className = 'role-context-banner repartidor-view';
        icon.textContent = '🛵';
        title.textContent = `Hoja de Ruta Personal • ${escapeHtml(user.nombre)}`;
        desc.textContent = 'Estás visualizando únicamente los pedidos asignados a tu vehículo. Puedes actualizar su estado y registrar incidencias.';
    } else {
        banner.className = 'role-context-banner';
        icon.textContent = '📋';
        title.textContent = 'Vista General de Despachos (Encargado)';
        desc.textContent = 'Monitoreo global de toda la flota, asignación de pedidos pendientes y métricas de operación.';
    }
}

function updateDisponibilidadBtn(disponible) {
    const btn = document.getElementById('btn-toggle-disponibilidad');
    if (!btn) return;

    if (disponible) {
        btn.className = 'btn btn-sm btn-disp-online';
        btn.innerHTML = '<span class="badge-dot" style="background:#10B981;"></span> <span id="text-disponibilidad">Disponible</span>';
    } else {
        btn.className = 'btn btn-sm btn-disp-offline';
        btn.innerHTML = '<span class="badge-dot" style="background:#EF4444;"></span> <span id="text-disponibilidad">En Pausa</span>';
    }
}

document.getElementById('btn-toggle-disponibilidad')?.addEventListener('click', async () => {
    if (!state.currentUser || state.currentUser.rol !== 'Repartidor') return;
    const nuevoEstado = !state.disponible;
    try {
        await api.repartidores.setDisponibilidad(state.currentUser.id, nuevoEstado);
        state.disponible = nuevoEstado;
        updateDisponibilidadBtn(nuevoEstado);
        showToast(`Disponibilidad cambiada a ${nuevoEstado ? 'Disponible' : 'En Pausa'}`, 'info');
    } catch (err) {
        showToast(err.message, 'error');
    }
});

function renderUnauthenticatedState() {
    // Resetear KPIs numéricos
    document.getElementById('kpi-total').textContent = '0';
    document.getElementById('kpi-en-camino').textContent = '0';
    document.getElementById('kpi-entregados').textContent = '0';
    document.getElementById('kpi-incidencias').textContent = '0';

    // Ocultar banner contextual y botón de disponibilidad
    const banner = document.getElementById('role-context-banner');
    if (banner) banner.style.display = 'none';
    const btnDisponibilidad = document.getElementById('btn-toggle-disponibilidad');
    if (btnDisponibilidad) btnDisponibilidad.style.display = 'none';

    // Tablero principal de pedidos
    const tbody = document.getElementById('pedidos-table-body');
    if (tbody) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7" class="empty-state">
                    <div class="empty-icon">🔐</div>
                    <p style="font-weight: 600; font-size: 1.05rem; color: var(--text-main); margin-bottom: 0.35rem;">Sesión no iniciada</p>
                    <p style="color: var(--text-muted); font-size: 0.85rem; margin-bottom: 1.25rem;">Debe iniciar sesión para acceder al tablero de pedidos y seguimiento operativo.</p>
                    <button class="btn btn-primary btn-sm" id="btn-login-empty">🔐 Iniciar Sesión</button>
                </td>
            </tr>
        `;
        document.getElementById('btn-login-empty')?.addEventListener('click', () => {
            openModal('modal-login');
        });
    }

    // Historial
    const timeline = document.getElementById('historial-timeline');
    if (timeline) {
        timeline.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">🔒</div>
                <p>Inicie sesión para consultar el historial de entregas.</p>
            </div>
        `;
    }

    // Métricas LINQ
    const conteoIncidencias = document.getElementById('conteo-incidencias-list');
    if (conteoIncidencias) {
        conteoIncidencias.innerHTML = '<li class="metric-list-item" style="color:var(--text-muted); font-size: 0.85rem;">Inicie sesión para ver métricas.</li>';
    }
    const conteoRepartidores = document.getElementById('conteo-repartidores-list');
    if (conteoRepartidores) {
        conteoRepartidores.innerHTML = '<li class="metric-list-item" style="color:var(--text-muted); font-size: 0.85rem;">Inicie sesión para ver métricas.</li>';
    }
}

document.getElementById('btn-login-trigger').addEventListener('click', () => {
    openModal('modal-login');
});

// Configurar accesos rápidos de prueba por rol
document.getElementById('btn-demo-encargado')?.addEventListener('click', () => {
    document.getElementById('login-email').value = 'admin@rutasegura.com';
    document.getElementById('login-password').value = 'admin123';
});

document.getElementById('btn-demo-lucas')?.addEventListener('click', () => {
    document.getElementById('login-email').value = 'lucas@rutasegura.com';
    document.getElementById('login-password').value = 'repartidor123';
});

document.getElementById('btn-demo-martin')?.addEventListener('click', () => {
    document.getElementById('login-email').value = 'martin@rutasegura.com';
    document.getElementById('login-password').value = 'repartidor123';
});

document.getElementById('btn-demo-carlos')?.addEventListener('click', () => {
    document.getElementById('login-email').value = 'carlos@rutasegura.com';
    document.getElementById('login-password').value = 'repartidor123';
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
        showToast(`Bienvenido/a, ${user.nombre || user.email} (${user.rol})`, 'success');
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
        state.pedidos = [];
        state.repartidores = [];
        state.historial = [];
        renderUnauthenticatedState();
        showToast('Sesión cerrada correctamente', 'info');
    } catch (err) {
        showToast(err.message, 'error');
    }
});

// ====================================================================
// CARGA DE DATOS & KPIS
// ====================================================================
async function loadInitialData() {
    if (!state.currentUser) {
        renderUnauthenticatedState();
        return;
    }
    // Si es repartidor, sincronizar disponibilidad desde el servidor
    if (state.currentUser.rol === 'Repartidor' && state.currentUser.id) {
        try {
            const repartidorData = await api.repartidores.getById(state.currentUser.id);
            if (repartidorData) {
                state.disponible = repartidorData.disponible;
                updateDisponibilidadBtn(state.disponible);
            }
        } catch {
            // Silenciar error; usar valor por defecto
        }
    }
    await Promise.allSettled([
        loadPedidos(),
        loadRepartidores(),
        loadHistorial(),
        loadMetricasLinq()
    ]);
}

async function loadPedidos() {
    if (!state.currentUser) return;
    try {
        state.pedidos = await api.pedidos.getAll();
        updateKpis();
        renderPedidos();
    } catch (err) {
        if (err.status === 401) {
            setLoggedUser(null);
            renderUnauthenticatedState();
            return;
        }
        renderEmptyState(err.message);
    }
}

async function loadRepartidores() {
    if (!state.currentUser) return;
    try {
        state.repartidores = await api.repartidores.getAll();
        populateRepartidoresSelect();
    } catch (err) {
        if (err.status === 401) return;
        console.warn('No se pudieron cargar repartidores:', err.message);
    }
}

function updateKpis() {
    const isRepartidor = (state.currentUser?.rol === 'Repartidor');

    // Adaptar títulos de KPIs según el rol
    const totalTitle = document.querySelector('#kpi-total')?.previousElementSibling;
    const caminoTitle = document.querySelector('#kpi-en-camino')?.previousElementSibling;
    const entregadosTitle = document.querySelector('#kpi-entregados')?.previousElementSibling;
    const incidenciasTitle = document.querySelector('#kpi-incidencias')?.previousElementSibling;

    if (totalTitle) totalTitle.textContent = isRepartidor ? 'Mis Pedidos' : 'Total Pedidos';
    if (caminoTitle) caminoTitle.textContent = isRepartidor ? 'Mis En Camino' : 'En Camino';
    if (entregadosTitle) entregadosTitle.textContent = isRepartidor ? 'Mis Entregados' : 'Entregados';
    if (incidenciasTitle) incidenciasTitle.textContent = isRepartidor ? 'Mis Incidencias' : 'Con Incidencias';

    // Para el repartidor, calcular sobre su propia carga de trabajo
    const pedidosRelevantes = isRepartidor
        ? state.pedidos.filter(p => p.repartidorId === state.currentUser.id)
        : state.pedidos;

    const total = pedidosRelevantes.length;
    const enCamino = pedidosRelevantes.filter(p => p.estado === 'EnCamino' || p.estado === 3).length;
    const entregados = pedidosRelevantes.filter(p => p.estado === 'Entregado' || p.estado === 4).length;
    const incidencias = pedidosRelevantes.filter(p => (p.incidencias && p.incidencias.length > 0) || p.estado === 'ConInconveniente' || p.estado === 5).length;

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

    const isRepartidor = (state.currentUser?.rol === 'Repartidor');

    // Si es Repartidor, filtramos exclusivamente sus pedidos asignados
    const pedidosBase = isRepartidor
        ? state.pedidos.filter(p => p.repartidorId === state.currentUser.id)
        : state.pedidos;

    let pedidosFiltrados = pedidosBase;
    if (state.filtroActual !== 'todos') {
        pedidosFiltrados = pedidosBase.filter(p => {
            const estadoStr = getEstadoString(p.estado);
            return estadoStr.toLowerCase() === state.filtroActual.toLowerCase();
        });
    }

    if (pedidosFiltrados.length === 0) {
        const mensajeVacio = isRepartidor
            ? (pedidosBase.length === 0 
                ? 'No tienes pedidos asignados actualmente. El encargado te asignará pedidos cuando ingresen despachos.' 
                : `No tienes pedidos con el estado "${state.filtroActual}".`)
            : `No se encontraron pedidos con el filtro "${state.filtroActual}".`;

        tbody.innerHTML = `
            <tr>
                <td colspan="7" class="empty-state">
                    <div class="empty-icon">${isRepartidor ? '🛵' : '📭'}</div>
                    <p>${escapeHtml(mensajeVacio)}</p>
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

        // Columna de repartidor adaptada al rol
        let repartidorHtml = '';
        if (isRepartidor) {
            repartidorHtml = `<span style="color:var(--status-entregado); font-weight:600;">🛵 A tu cargo</span>`;
        } else if (repartidorNombre) {
            repartidorHtml = `
                <div><span style="color:var(--text-main); font-weight:500;">👤 ${escapeHtml(repartidorNombre)}</span></div>
                ${(estadoStr !== 'Entregado' && estadoStr !== 'Cancelado') ? `<button class="btn btn-link btn-xs btn-asignar" data-id="${p.id}" style="padding:0; font-size:0.75rem; color:var(--primary); text-decoration:underline;">Reasignar</button>` : ''}
            `;
        } else {
            repartidorHtml = `<button class="btn btn-secondary btn-sm btn-asignar" data-id="${p.id}">+ Asignar</button>`;
        }

        tr.innerHTML = `
            <td><strong style="color:var(--primary)">#${p.id}</strong></td>
            <td>
                <div><strong>${escapeHtml(p.descripcion)}</strong></div>
                ${p.clienteNombre ? `<div style="font-size:0.75rem; color:var(--text-muted)">Cliente: ${escapeHtml(p.clienteNombre)}</div>` : ''}
                ${cantIncidencias > 0 ? `<div class="incident-indicator">⚠️ ${cantIncidencias} incidencia(s)</div>` : ''}
            </td>
            <td>${formatDate(p.fechaPactada)}</td>
            <td><span class="priority-${prioridadStr.toLowerCase()}">${prioridadStr}</span></td>
            <td>${repartidorHtml}</td>
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
                        ${!isRepartidor ? `
                        <button class="btn btn-secondary btn-sm btn-editar-pedido" data-id="${p.id}">
                            ✏️ Editar
                        </button>
                        ` : ''}
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

    tbody.querySelectorAll('.btn-editar-pedido').forEach(btn => {
        btn.addEventListener('click', () => {
            const pedidoId = btn.getAttribute('data-id');
            abrirModalEditar(pedidoId);
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
    if (!state.currentUser) {
        showToast('Debe iniciar sesión para ver los pedidos', 'warning');
        openModal('modal-login');
        return;
    }
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

    const isRepartidor = (state.currentUser?.rol === 'Repartidor');
    const select = document.getElementById('select-nuevo-estado');
    select.innerHTML = `
        <option value="EnCamino">🚴 En Camino (Despachado)</option>
        <option value="Entregado">✅ Entregado (Entrega exitosa)</option>
        ${!isRepartidor ? '<option value="Cancelado">❌ Cancelado</option>' : ''}
    `;

    // Si está Asignado, sugerir EnCamino; si está EnCamino, sugerir Entregado
    if (estadoActual === 'Asignado') {
        select.value = 'EnCamino';
    } else if (estadoActual === 'EnCamino') {
        select.value = 'Entregado';
    } else if (estadoActual === 'ConInconveniente') {
        select.value = 'EnCamino';
    } else {
        select.value = 'EnCamino';
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
async function populateRepartidoresSelect() {
    try {
        const cargaRepartidores = await api.repartidores.getCarga();
        state.repartidoresCarga = cargaRepartidores;

        const selects = [
            document.getElementById('select-repartidor'),
            document.getElementById('nuevo-pedido-repartidor')
        ];

        selects.forEach(select => {
            if (!select) return;
            const isNuevoPedido = (select.id === 'nuevo-pedido-repartidor');
            select.innerHTML = isNuevoPedido 
                ? '<option value="">-- Dejar Pendiente de Asignación --</option>'
                : '<option value="">-- Seleccionar Repartidor --</option>';

            cargaRepartidores.forEach(r => {
                const option = document.createElement('option');
                option.value = r.id;
                let estadoStr = '';
                if (!r.disponible) {
                    estadoStr = `[En Pausa] (${r.cargaActiva}/${r.capacidadMaxima} pedidos)`;
                    option.disabled = true;
                } else if (!r.tieneCapacidad) {
                    estadoStr = `[Capacidad Llena] (${r.cargaActiva}/${r.capacidadMaxima} pedidos)`;
                    option.disabled = true;
                } else {
                    estadoStr = `[Disponible] (${r.cargaActiva}/${r.capacidadMaxima} pedidos)`;
                }
                option.textContent = `${r.nombre} (${r.vehiculo || 'Sin vehículo'}) • ${estadoStr}`;
                select.appendChild(option);
            });
        });
    } catch (err) {
        console.warn('Error al cargar carga de repartidores:', err.message);
    }
}

async function abrirModalAsignar(pedidoId) {
    if (state.currentUser?.rol === 'Repartidor') {
        showToast('Solo el Encargado puede asignar repartidores.', 'error');
        return;
    }
    document.getElementById('modal-asignar-id').textContent = pedidoId;
    document.getElementById('asignar-pedido-id').value = pedidoId;
    await populateRepartidoresSelect();
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
// MODAL: CREAR NUEVO PEDIDO
// ====================================================================
document.getElementById('btn-nuevo-pedido')?.addEventListener('click', async () => {
    await abrirModalNuevoPedido();
});

async function abrirModalNuevoPedido() {
    if (state.currentUser?.rol === 'Repartidor') {
        showToast('Solo el Encargado puede crear pedidos.', 'error');
        return;
    }

    const form = document.getElementById('form-nuevo-pedido');
    if (form) form.reset();

    // Cargar clientes
    try {
        const clientes = await api.clientes.getAll();
        const selectCliente = document.getElementById('nuevo-pedido-cliente');
        selectCliente.innerHTML = '<option value="">-- Seleccionar Cliente --</option>';
        clientes.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = `${c.nombre} (${c.direccion})`;
            selectCliente.appendChild(opt);
        });
    } catch (err) {
        console.warn('Error al cargar clientes:', err.message);
    }

    // Setear fecha por defecto a hoy + 3 horas
    const now = new Date();
    now.setHours(now.getHours() + 3);
    const pad = (n) => String(n).padStart(2, '0');
    const dtLocal = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}`;
    const fechaInput = document.getElementById('nuevo-pedido-fecha');
    if (fechaInput) fechaInput.value = dtLocal;

    // Cargar repartidores con su capacidad y disponibilidad
    await populateRepartidoresSelect();

    openModal('modal-nuevo-pedido');
}

document.getElementById('form-nuevo-pedido')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const clienteId = parseInt(document.getElementById('nuevo-pedido-cliente').value, 10);
    const descripcion = document.getElementById('nuevo-pedido-desc').value.trim();
    const fechaPactadaVal = document.getElementById('nuevo-pedido-fecha').value;
    const prioridad = document.getElementById('nuevo-pedido-prioridad').value;
    const repartidorIdVal = document.getElementById('nuevo-pedido-repartidor').value;
    const observaciones = document.getElementById('nuevo-pedido-obs').value.trim();
    const btn = document.getElementById('btn-guardar-nuevo-pedido');

    if (!clienteId || !descripcion || !fechaPactadaVal) {
        showToast('Complete los campos obligatorios.', 'error');
        return;
    }

    try {
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner"></span> Creando...';
        const fechaPactada = new Date(fechaPactadaVal).toISOString();
        const repartidorId = repartidorIdVal ? parseInt(repartidorIdVal, 10) : null;
        const res = await api.pedidos.create({
            clienteId,
            descripcion,
            fechaPactada,
            prioridad,
            observaciones: observaciones || null
        });
        const nuevoId = res?.id || (typeof res === 'number' ? res : null);
        if (repartidorId && nuevoId) {
            await api.repartidores.asignar(repartidorId, nuevoId);
        }
        showToast(`Pedido #${nuevoId || ''} creado exitosamente`, 'success');
        closeModal('modal-nuevo-pedido');
        await loadInitialData();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Crear Pedido';
    }
});

// ====================================================================
// MODAL: MODIFICAR PEDIDO EXISTENTE
// ====================================================================
function abrirModalEditar(pedidoId) {
    if (state.currentUser?.rol === 'Repartidor') {
        showToast('Solo el Encargado puede modificar pedidos.', 'error');
        return;
    }

    const pedido = state.pedidos.find(p => p.id === parseInt(pedidoId, 10));
    if (!pedido) {
        showToast('No se encontró el pedido a editar.', 'error');
        return;
    }

    document.getElementById('editar-pedido-id-text').textContent = pedido.id;
    document.getElementById('editar-pedido-id').value = pedido.id;
    document.getElementById('editar-pedido-desc').value = pedido.descripcion || '';

    if (pedido.fechaPactada) {
        const d = new Date(pedido.fechaPactada);
        const pad = (n) => String(n).padStart(2, '0');
        const dtLocal = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
        document.getElementById('editar-pedido-fecha').value = dtLocal;
    }

    document.getElementById('editar-pedido-prioridad').value = getPrioridadString(pedido.prioridad);
    document.getElementById('editar-pedido-obs').value = pedido.observaciones || '';

    openModal('modal-editar-pedido');
}

document.getElementById('form-editar-pedido')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const id = parseInt(document.getElementById('editar-pedido-id').value, 10);
    const descripcion = document.getElementById('editar-pedido-desc').value.trim();
    const fechaPactadaVal = document.getElementById('editar-pedido-fecha').value;
    const prioridad = document.getElementById('editar-pedido-prioridad').value;
    const observaciones = document.getElementById('editar-pedido-obs').value.trim();
    const btn = document.getElementById('btn-guardar-edicion');

    try {
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner"></span> Guardando...';
        const fechaPactada = new Date(fechaPactadaVal).toISOString();
        await api.pedidos.update(id, {
            descripcion,
            fechaPactada,
            prioridad,
            observaciones: observaciones || null
        });
        showToast(`Pedido #${id} actualizado correctamente`, 'success');
        closeModal('modal-editar-pedido');
        await loadInitialData();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Guardar Cambios';
    }
});

// ====================================================================
// HISTORIAL DE ENTREGAS & TRAZABILIDAD (RF14)
// ====================================================================
async function loadHistorial(filtros = {}) {
    if (!state.currentUser) return;
    try {
        const queryFiltros = { ...filtros };
        // Si el usuario es Repartidor, restringir historial a sus entregas
        if (state.currentUser.rol === 'Repartidor') {
            queryFiltros.repartidorId = state.currentUser.id;
        }
        state.historial = await api.seguimiento.getHistorial(queryFiltros);
        renderHistorial();
    } catch (err) {
        if (err.status === 401) return;
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
    if (!state.currentUser) return;
    try {
        const [conteosIncidencias, conteosRepartidores] = await Promise.all([
            api.seguimiento.getConteoIncidencias(),
            api.seguimiento.getEntregasPorRepartidor()
        ]);

        renderConteoList('conteo-incidencias-list', conteosIncidencias, 'tipo de incidencia');
        renderConteoList('conteo-repartidores-list', conteosRepartidores, 'repartidor con entregas');
        await renderFlotaRepartidores();
    } catch (err) {
        if (err.status === 401) return;
        console.warn('Error al cargar métricas LINQ:', err.message);
    }
}

async function renderFlotaRepartidores() {
    const list = document.getElementById('flota-repartidores-list');
    if (!list) return;

    try {
        const carga = await api.repartidores.getCarga();
        list.innerHTML = '';
        if (!carga || carga.length === 0) {
            list.innerHTML = '<div class="empty-state">No hay repartidores registrados.</div>';
            return;
        }

        carga.forEach(r => {
            const item = document.createElement('div');
            item.className = 'key-value-row';
            item.style.padding = '0.65rem 0';
            item.style.borderBottom = '1px solid var(--border-color)';
            item.style.display = 'flex';
            item.style.justifyContent = 'space-between';
            item.style.alignItems = 'center';

            const pct = Math.min(100, Math.round((r.cargaActiva / r.capacidadMaxima) * 100));
            const statusColor = !r.disponible ? '#EF4444' : (!r.tieneCapacidad ? '#F59E0B' : '#10B981');
            const statusBg = !r.disponible ? 'rgba(239, 68, 68, 0.12)' : (!r.tieneCapacidad ? 'rgba(245, 158, 11, 0.12)' : 'rgba(16, 185, 129, 0.12)');
            const statusText = !r.disponible ? 'En Pausa' : (!r.tieneCapacidad ? 'Capacidad Llena' : 'Disponible');

            item.innerHTML = `
                <div>
                    <div style="font-weight: 600; color: var(--text-main); font-size: 0.88rem;">
                        ${escapeHtml(r.nombre)} <span style="font-size:0.75rem; color:var(--text-muted); font-weight:normal;">(${escapeHtml(r.vehiculo || 'Sin vehículo')})</span>
                    </div>
                    <div style="font-size: 0.75rem; color: var(--text-muted); margin-top: 3px;">
                        Carga activa: <strong>${r.cargaActiva}</strong> / ${r.capacidadMaxima} pedidos (${pct}%)
                    </div>
                </div>
                <div>
                    <span class="badge" style="background: ${statusBg}; color: ${statusColor}; font-weight: 600; font-size: 0.75rem; padding: 0.25rem 0.6rem; border-radius: 4px; display:inline-flex; align-items:center; gap:0.35rem;">
                        <span class="badge-dot" style="background: ${statusColor}; width:6px; height:6px; border-radius:50%;"></span>
                        ${statusText}
                    </span>
                </div>
            `;
            list.appendChild(item);
        });
    } catch (err) {
        list.innerHTML = `<div class="empty-state">Error al cargar flota: ${escapeHtml(err.message)}</div>`;
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
        const isMe = (state.currentUser?.rol === 'Repartidor' && key === state.currentUser.nombre);
        const item = document.createElement('div');
        item.className = 'key-value-item';
        item.innerHTML = `
            <span>${escapeHtml(key)} ${isMe ? '<strong style="color:var(--status-entregado); font-size:0.75rem; margin-left:0.35rem;">(Vos)</strong>' : ''}</span>
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

        if (!state.currentUser) {
            renderUnauthenticatedState();
            return;
        }

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
    const user = await checkAuthSession();
    if (user) {
        await loadInitialData();
    } else {
        renderUnauthenticatedState();
    }
});
