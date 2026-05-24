// ═══════════════════════════════════════════════════════════════
// AquaSchedule v2 — Frontend Application
// ═══════════════════════════════════════════════════════════════

const API_BASE = '/api';
let currentUser = null;
let calendarInstance = null;

// ─── Page Detection ───────────────────────────────────────────
const isLoginPage = !document.getElementById('dashboardSection') && document.getElementById('authSection');
const isDashboardPage = document.getElementById('sectionTurmas') != null;

// ─── Init ─────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    const saved = localStorage.getItem('aquaUser');

    if (isLoginPage) {
        // Check for reset token in URL
        const params = new URLSearchParams(window.location.search);
        const resetToken = params.get('resetToken');
        if (resetToken) {
            showResetPassword(resetToken);
            return;
        }
        // If already logged in, redirect to dashboard
        if (saved) {
            window.location.href = 'dashboard.html';
        }
    } else if (isDashboardPage) {
        if (!saved) {
            window.location.href = '/';
            return;
        }
        currentUser = JSON.parse(saved);
        initDashboard();
    }
});

// ═══════════════════════════════════════════════════════════════
// AUTH — Login
// ═══════════════════════════════════════════════════════════════

async function handleLogin(e) {
    e.preventDefault();
    const btn = document.getElementById('btnLogin');
    setBtnLoading(btn, true);

    try {
        const res = await fetch(`${API_BASE}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                email: document.getElementById('loginEmail').value,
                senha: document.getElementById('loginSenha').value
            })
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'E-mail ou senha inválidos.');
        }

        currentUser = await res.json();
        localStorage.setItem('aquaUser', JSON.stringify(currentUser));
        window.location.href = 'dashboard.html';
    } catch (err) {
        showAuthAlert(err.message, 'danger');
    } finally {
        setBtnLoading(btn, false);
    }
}

// ═══════════════════════════════════════════════════════════════
// AUTH — Forgot / Reset Password
// ═══════════════════════════════════════════════════════════════

function showForgotPassword(e) {
    e?.preventDefault();
    document.getElementById('loginView').style.display = 'none';
    document.getElementById('forgotView').style.display = '';
    document.getElementById('resetView').style.display = 'none';
    hideAuthAlert();
}

function showLogin(e) {
    e?.preventDefault();
    document.getElementById('loginView').style.display = '';
    document.getElementById('forgotView').style.display = 'none';
    document.getElementById('resetView').style.display = 'none';
    hideAuthAlert();
}

function showResetPassword(token) {
    document.getElementById('loginView').style.display = 'none';
    document.getElementById('forgotView').style.display = 'none';
    document.getElementById('resetView').style.display = '';
    document.getElementById('resetView').dataset.token = token;
}

async function handleForgotPassword(e) {
    e.preventDefault();
    const btn = document.getElementById('btnForgotPassword');
    setBtnLoading(btn, true);

    try {
        await fetch(`${API_BASE}/auth/forgot-password`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: document.getElementById('forgotEmail').value })
        });
        showAuthAlert('Se o e-mail estiver cadastrado, você receberá um link de recuperação. Verifique sua caixa de entrada.', 'success');
    } catch (err) {
        showAuthAlert('Erro ao processar a solicitação.', 'danger');
    } finally {
        setBtnLoading(btn, false);
    }
}

async function handleResetPassword(e) {
    e.preventDefault();
    const btn = document.getElementById('btnResetPassword');
    setBtnLoading(btn, true);

    try {
        const token = document.getElementById('resetView').dataset.token;
        const res = await fetch(`${API_BASE}/auth/reset-password`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                token: token,
                novaSenha: document.getElementById('resetSenha').value
            })
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Token inválido ou expirado.');
        }

        showAuthAlert('Senha redefinida com sucesso! Faça login com sua nova senha.', 'success');
        setTimeout(() => {
            window.location.href = '/';
        }, 2000);
    } catch (err) {
        showAuthAlert(err.message, 'danger');
    } finally {
        setBtnLoading(btn, false);
    }
}

function logout() {
    currentUser = null;
    localStorage.removeItem('aquaUser');
    window.location.href = '/';
}

// ═══════════════════════════════════════════════════════════════
// DASHBOARD
// ═══════════════════════════════════════════════════════════════

function initDashboard() {
    // Nav user area
    document.getElementById('navUserName').textContent = currentUser.nome;
    const roleMap = { Admin: '👑 Admin', Professor: '👨‍🏫 Professor', Aluno: '🏊 Aluno' };
    document.getElementById('navUserRole').textContent = roleMap[currentUser.role] || currentUser.role;
    document.getElementById('statRole').textContent = currentUser.role;

    const isAdmin = currentUser.role === 'Admin';
    const isProfessor = currentUser.role === 'Professor';

    // Role visibility
    document.querySelectorAll('.admin-only').forEach(el => {
        el.style.display = isAdmin ? '' : 'none';
    });
    document.querySelectorAll('.professor-only').forEach(el => {
        el.style.display = (isProfessor || isAdmin) ? '' : 'none';
    });
    document.querySelectorAll('.aluno-only').forEach(el => {
        el.style.display = (!isProfessor && !isAdmin) ? '' : 'none';
    });

    // Labels
    if (isProfessor || isAdmin) {
        document.getElementById('reservasTitle').textContent = 'Todas as Reservas';
        document.getElementById('statReservasLabel').textContent = 'Total de Reservas';
    }

    // Load data
    initCalendar();
    loadReservas();
    if (isAdmin) loadUsuarios();
}

// ─── Section Switching ────────────────────────────────────────
function switchSection(name) {
    document.querySelectorAll('.content-section').forEach(s => s.classList.remove('active'));
    document.querySelectorAll('.nav-tab').forEach(t => t.classList.remove('active'));

    const section = document.getElementById('section' + name.charAt(0).toUpperCase() + name.slice(1));
    if (section) section.classList.add('active');

    const tab = document.querySelector(`.nav-tab[data-section="${name}"]`);
    if (tab) tab.classList.add('active');
}

// ═══════════════════════════════════════════════════════════════
// TURMAS — FullCalendar
// ═══════════════════════════════════════════════════════════════

function initCalendar() {
    const calendarEl = document.getElementById('calendar');
    calendarInstance = new FullCalendar.Calendar(calendarEl, {
        locale: 'pt-br',
        initialView: 'dayGridMonth',
        headerToolbar: { left: 'prev,next today', center: 'title', right: 'dayGridMonth,timeGridWeek,listWeek' },
        height: 'auto',
        events: fetchTurmas,
        eventClick: handleEventClick,
        dateClick: handleDateClick
    });
    calendarInstance.render();
}

async function fetchTurmas(info, successCallback) {
    try {
        const res = await apiFetch('/turmas');
        const turmas = await res.json();
        document.getElementById('statTurmas').textContent = turmas.length;

        const events = turmas.map(t => ({
            id: t.id,
            title: `${t.nome} (${t.vagasDisponiveis}/${t.capacidadeMaxima})`,
            start: t.dataHoraInicio,
            end: t.dataHoraFim,
            className: t.vagasDisponiveis > 0 ? 'event-vagas' : 'event-lotada',
            extendedProps: t
        }));
        successCallback(events);
    } catch (err) {
        console.error('Erro ao carregar turmas:', err);
        successCallback([]);
    }
}

function handleEventClick(info) {
    const t = info.event.extendedProps;
    const inicio = new Date(t.dataHoraInicio).toLocaleString('pt-BR');
    const fim = new Date(t.dataHoraFim).toLocaleString('pt-BR');
    const vagasBadge = t.vagasDisponiveis > 0
        ? `<span class="badge-vagas disponivel">${t.vagasDisponiveis}/${t.capacidadeMaxima} vagas</span>`
        : `<span class="badge-vagas lotada">Lotada</span>`;

    const modalBody = document.getElementById('eventoModalBody');
    modalBody.innerHTML = `
        <h5 class="fw-700 mb-2">${escapeHtml(t.nome)}</h5>
        <p class="text-secondary mb-3">${escapeHtml(t.descricao || '')}</p>
        <div class="d-flex flex-column gap-2">
            <div><i class="bi bi-tag me-2 text-accent"></i>${escapeHtml(t.modalidade)}</div>
            <div><i class="bi bi-clock me-2 text-accent"></i>${inicio} → ${fim}</div>
            <div><i class="bi bi-person me-2 text-accent"></i>${escapeHtml(t.professorNome)}</div>
            <div><i class="bi bi-people me-2 text-accent"></i>${vagasBadge}</div>
        </div>`;

    const modalFooter = document.getElementById('eventoModalFooter');
    modalFooter.innerHTML = '';

    const isProfOrAdmin = currentUser.role === 'Professor' || currentUser.role === 'Admin';

    if (isProfOrAdmin) {
        modalFooter.innerHTML = `
            <button class="btn btn-outline-warning btn-sm rounded-pill px-3" onclick="editarTurma(${t.id})">
                <i class="bi bi-pencil me-1"></i>Editar
            </button>
            <button class="btn btn-outline-danger btn-sm rounded-pill px-3" onclick="deletarTurma(${t.id}, '${escapeHtml(t.nome)}')">
                <i class="bi bi-trash me-1"></i>Deletar
            </button>`;
    } else if (currentUser.role === 'Aluno' && t.vagasDisponiveis > 0) {
        modalFooter.innerHTML = `
            <button class="btn btn-accent btn-sm rounded-pill px-3" onclick="criarReserva(${t.id})">
                <i class="bi bi-bookmark-plus me-1"></i>Reservar Vaga
            </button>`;
    }

    document.getElementById('eventoModalTitle').textContent = t.nome;
    new bootstrap.Modal(document.getElementById('eventoModal')).show();
}

function handleDateClick(info) {
    if (currentUser.role !== 'Professor' && currentUser.role !== 'Admin') return;
    document.getElementById('turmaId').value = '';
    document.getElementById('turmaForm').reset();
    document.getElementById('turmaInicio').value = info.dateStr + 'T08:00';
    document.getElementById('turmaFim').value = info.dateStr + 'T09:00';
    document.getElementById('turmaModalLabel').innerHTML = '<i class="bi bi-water me-2 text-accent"></i>Nova Turma';
    new bootstrap.Modal(document.getElementById('turmaModal')).show();
}

async function handleTurmaSubmit(e) {
    e.preventDefault();
    const turmaId = document.getElementById('turmaId').value;
    const isEdit = !!turmaId;

    const payload = {
        nome: document.getElementById('turmaNome').value,
        descricao: document.getElementById('turmaDescricao').value,
        modalidade: document.getElementById('turmaModalidade').value,
        dataHoraInicio: document.getElementById('turmaInicio').value,
        dataHoraFim: document.getElementById('turmaFim').value,
        capacidadeMaxima: parseInt(document.getElementById('turmaCapacidade').value)
    };

    try {
        const url = isEdit ? `/turmas/${turmaId}` : '/turmas';
        const method = isEdit ? 'PUT' : 'POST';
        const res = await apiFetch(url, { method, body: JSON.stringify(payload) });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Erro ao salvar turma.');
        }

        bootstrap.Modal.getInstance(document.getElementById('turmaModal')).hide();
        showToast(isEdit ? 'Turma atualizada! ✅' : 'Turma criada! ✅', 'success');
        calendarInstance.refetchEvents();
        loadReservas();
    } catch (err) {
        showToast(err.message, 'danger');
    }
}

async function editarTurma(id) {
    try {
        bootstrap.Modal.getInstance(document.getElementById('eventoModal'))?.hide();
        const res = await apiFetch(`/turmas/${id}`);
        const t = await res.json();

        document.getElementById('turmaId').value = t.id;
        document.getElementById('turmaNome').value = t.nome;
        document.getElementById('turmaDescricao').value = t.descricao || '';
        document.getElementById('turmaModalidade').value = t.modalidade;
        document.getElementById('turmaInicio').value = t.dataHoraInicio?.substring(0, 16);
        document.getElementById('turmaFim').value = t.dataHoraFim?.substring(0, 16);
        document.getElementById('turmaCapacidade').value = t.capacidadeMaxima;
        document.getElementById('turmaModalLabel').innerHTML = '<i class="bi bi-pencil me-2 text-accent"></i>Editar Turma';

        new bootstrap.Modal(document.getElementById('turmaModal')).show();
    } catch (err) {
        showToast('Erro ao carregar turma.', 'danger');
    }
}

async function deletarTurma(id, nome) {
    bootstrap.Modal.getInstance(document.getElementById('eventoModal'))?.hide();
    if (!confirm(`Tem certeza que deseja deletar "${nome}"?`)) return;

    try {
        const res = await apiFetch(`/turmas/${id}`, { method: 'DELETE' });
        if (!res.ok && res.status !== 204) throw new Error('Erro ao deletar.');
        showToast('Turma deletada. 🗑️', 'warning');
        calendarInstance.refetchEvents();
        loadReservas();
    } catch (err) { showToast(err.message, 'danger'); }
}

document.getElementById('turmaModal')?.addEventListener('hidden.bs.modal', () => {
    document.getElementById('turmaForm').reset();
    document.getElementById('turmaId').value = '';
    document.getElementById('turmaModalLabel').innerHTML = '<i class="bi bi-water me-2 text-accent"></i>Nova Turma';
});

// ═══════════════════════════════════════════════════════════════
// RESERVAS
// ═══════════════════════════════════════════════════════════════

async function loadReservas() {
    try {
        const res = await apiFetch('/reservas');
        const reservas = await res.json();

        const ativas = reservas.filter(r => r.status === 'Ativa');
        document.getElementById('statReservas').textContent = ativas.length;

        const tbody = document.getElementById('reservasBody');
        const isProfOrAdmin = currentUser.role === 'Professor' || currentUser.role === 'Admin';

        if (reservas.length === 0) {
            tbody.innerHTML = `<tr><td colspan="${isProfOrAdmin ? 6 : 5}" class="text-center py-4 text-secondary">
                <i class="bi bi-bookmark fs-3 d-block mb-2"></i>Nenhuma reserva encontrada.</td></tr>`;
            return;
        }

        // Add Aluno column header for professor/admin
        const thead = document.querySelector('#reservasTable thead tr');
        if (isProfOrAdmin && !thead.querySelector('.col-aluno')) {
            const th = document.createElement('th');
            th.textContent = 'Aluno';
            th.className = 'col-aluno';
            thead.insertBefore(th, thead.children[1]);
        }

        tbody.innerHTML = reservas.map(r => {
            const statusBadge = r.status === 'Ativa'
                ? '<span class="badge-status ativa">● Ativa</span>'
                : '<span class="badge-status cancelada">● Cancelada</span>';

            const dataReserva = new Date(r.dataReserva).toLocaleDateString('pt-BR');
            const inicio = new Date(r.dataHoraInicio).toLocaleString('pt-BR');

            const alunoCol = isProfOrAdmin ? `<td>${escapeHtml(r.alunoNome)}</td>` : '';

            let actions = '';
            if (currentUser.role === 'Aluno' && r.status === 'Ativa') {
                actions = `<button class="btn-action cancelar" onclick="cancelarReserva(${r.id})" title="Cancelar">
                    <i class="bi bi-x-circle"></i></button>`;
            }

            return `<tr>
                <td class="fw-600">${escapeHtml(r.turmaNome)}</td>
                ${alunoCol}
                <td>${inicio}</td>
                <td>${dataReserva}</td>
                <td>${statusBadge}</td>
                <td class="text-end aluno-only">${actions}</td>
            </tr>`;
        }).join('');
    } catch (err) { showToast('Erro ao carregar reservas.', 'danger'); }
}

async function criarReserva(turmaId) {
    bootstrap.Modal.getInstance(document.getElementById('eventoModal'))?.hide();
    if (!confirm('Deseja reservar uma vaga nesta turma?')) return;

    try {
        const res = await apiFetch('/reservas', { method: 'POST', body: JSON.stringify({ turmaId }) });
        if (!res.ok) { const err = await res.json(); throw new Error(err.message); }
        showToast('Reserva realizada! 🎉', 'success');
        calendarInstance.refetchEvents();
        loadReservas();
    } catch (err) { showToast(err.message, 'danger'); }
}

async function cancelarReserva(id) {
    if (!confirm('Cancelar esta reserva?')) return;
    try {
        const res = await apiFetch(`/reservas/${id}`, { method: 'DELETE' });
        if (!res.ok && res.status !== 204) throw new Error('Erro ao cancelar.');
        showToast('Reserva cancelada. 🗑️', 'warning');
        calendarInstance.refetchEvents();
        loadReservas();
    } catch (err) { showToast(err.message, 'danger'); }
}

// ═══════════════════════════════════════════════════════════════
// USUARIOS — Admin Only
// ═══════════════════════════════════════════════════════════════

async function loadUsuarios() {
    try {
        const res = await apiFetch('/usuarios');
        const usuarios = await res.json();

        const alunos = usuarios.filter(u => u.role === 'Aluno');
        const profs = usuarios.filter(u => u.role === 'Professor');
        const elAlunos = document.getElementById('statAlunos');
        const elProfs = document.getElementById('statProfessores');
        if (elAlunos) elAlunos.textContent = alunos.length;
        if (elProfs) elProfs.textContent = profs.length;

        const tbody = document.getElementById('usuariosBody');
        if (usuarios.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5" class="text-center py-4 text-secondary">Nenhum usuário cadastrado.</td></tr>`;
            return;
        }

        tbody.innerHTML = usuarios.map(u => {
            const roleBadge = `<span class="badge-role ${u.role.toLowerCase()}">${u.role}</span>`;
            const data = new Date(u.dataCriacao).toLocaleDateString('pt-BR');
            const deleteBtn = u.role !== 'Admin'
                ? `<button class="btn-action deletar" onclick="deleteUsuario(${u.id}, '${escapeHtml(u.nome)}')" title="Excluir">
                    <i class="bi bi-trash"></i></button>`
                : '<span class="text-muted" title="Admin não pode ser excluído"><i class="bi bi-shield-lock"></i></span>';

            return `<tr>
                <td class="fw-600">${escapeHtml(u.nome)}</td>
                <td>${escapeHtml(u.email)}</td>
                <td>${roleBadge}</td>
                <td>${data}</td>
                <td class="text-end">${deleteBtn}</td>
            </tr>`;
        }).join('');
    } catch (err) { showToast('Erro ao carregar usuários.', 'danger'); }
}

async function handleCreateUsuario(e) {
    e.preventDefault();
    try {
        const res = await apiFetch('/usuarios', {
            method: 'POST',
            body: JSON.stringify({
                nome: document.getElementById('usuarioNome').value,
                email: document.getElementById('usuarioEmail').value,
                senha: document.getElementById('usuarioSenha').value,
                role: document.getElementById('usuarioRole').value
            })
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Erro ao criar usuário.');
        }

        bootstrap.Modal.getInstance(document.getElementById('usuarioModal')).hide();
        document.getElementById('usuarioForm').reset();
        showToast('Usuário cadastrado! Email de boas-vindas enviado. 📧', 'success');
        loadUsuarios();
    } catch (err) { showToast(err.message, 'danger'); }
}

async function deleteUsuario(id, nome) {
    if (!confirm(`Excluir o usuário "${nome}"?`)) return;
    try {
        const res = await apiFetch(`/usuarios/${id}`, { method: 'DELETE' });
        if (!res.ok && res.status !== 204) throw new Error('Erro ao excluir.');
        showToast('Usuário excluído. 🗑️', 'warning');
        loadUsuarios();
    } catch (err) { showToast(err.message, 'danger'); }
}

// ═══════════════════════════════════════════════════════════════
// UTILITIES
// ═══════════════════════════════════════════════════════════════

async function apiFetch(path, options = {}) {
    const headers = { 'Content-Type': 'application/json', ...options.headers };
    if (currentUser?.token) headers['Authorization'] = `Bearer ${currentUser.token}`;

    const res = await fetch(`${API_BASE}${path}`, { ...options, headers });

    if (res.status === 401) {
        showToast('Sessão expirada. Faça login novamente.', 'warning');
        logout();
        throw new Error('Sessão expirada.');
    }
    if (res.status === 403) {
        showToast('Sem permissão para esta ação.', 'danger');
        throw new Error('Permissão negada.');
    }
    return res;
}

function showAuthAlert(message, type) {
    const alert = document.getElementById('authAlert');
    if (!alert) return;
    alert.style.display = '';
    const bgClass = type === 'success'
        ? 'background: rgba(16,185,129,0.1); border: 1px solid rgba(16,185,129,0.3); color: #34d399;'
        : 'background: rgba(239,68,68,0.1); border: 1px solid rgba(239,68,68,0.3); color: #fca5a5;';
    alert.innerHTML = `<div class="alert mb-0" style="${bgClass} border-radius: 10px; font-size: 0.85rem; padding: 12px 16px;">
        ${escapeHtml(message)}</div>`;
}

function hideAuthAlert() {
    const alert = document.getElementById('authAlert');
    if (alert) alert.style.display = 'none';
}

function showToast(message, type = 'success') {
    const toast = document.getElementById('appToast');
    if (!toast) return;
    toast.className = `toast align-items-center border-0 bg-${type}-toast text-white`;
    document.getElementById('toastMessage').textContent = message;
    bootstrap.Toast.getOrCreateInstance(toast, { delay: 4000 }).show();
}

function setBtnLoading(btn, loading) {
    if (!btn) return;
    const text = btn.querySelector('.btn-text');
    const loader = btn.querySelector('.btn-loader');
    if (text) text.style.display = loading ? 'none' : '';
    if (loader) loader.style.display = loading ? '' : 'none';
    btn.disabled = loading;
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}