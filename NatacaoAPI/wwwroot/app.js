// ═══════════════════════════════════════════════════════════════
// AquaSchedule — Frontend Application
// Consome a API REST via fetch, gerencia autenticação JWT,
// e renderiza dinamicamente turmas e reservas.
// ═══════════════════════════════════════════════════════════════

const API_BASE = '/api';

// ─── Estado da Aplicação ──────────────────────────────────────
let currentUser = null; // { id, nome, email, role, token }

// ─── Inicialização ────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    // Verificar se há sessão salva
    const saved = localStorage.getItem('aquaUser');
    if (saved) {
        currentUser = JSON.parse(saved);
        showDashboard();
    }
});

// ═══════════════════════════════════════════════════════════════
// AUTENTICAÇÃO
// ═══════════════════════════════════════════════════════════════

async function handleLogin(e) {
    e.preventDefault();
    const btn = document.getElementById('btnLogin');
    btn.disabled = true;
    btn.innerHTML = '<div class="spinner-wave"><span></span><span></span><span></span><span></span></div>';

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
        showDashboard();
        showToast('Bem-vindo, ' + currentUser.nome + '! 🏊', 'success');
    } catch (err) {
        showAuthAlert(err.message, 'danger');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-box-arrow-in-right me-2"></i>Entrar';
    }
}

async function handleRegister(e) {
    e.preventDefault();
    const btn = document.getElementById('btnRegister');
    btn.disabled = true;
    btn.innerHTML = '<div class="spinner-wave"><span></span><span></span><span></span><span></span></div>';

    try {
        const res = await fetch(`${API_BASE}/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                nome: document.getElementById('regNome').value,
                email: document.getElementById('regEmail').value,
                senha: document.getElementById('regSenha').value,
                role: document.getElementById('regRole').value
            })
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Erro ao registrar.');
        }

        currentUser = await res.json();
        localStorage.setItem('aquaUser', JSON.stringify(currentUser));
        showDashboard();
        showToast('Conta criada com sucesso! 🎉', 'success');
    } catch (err) {
        showAuthAlert(err.message, 'danger');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-person-plus me-2"></i>Criar Conta';
    }
}

function logout() {
    currentUser = null;
    localStorage.removeItem('aquaUser');
    document.getElementById('authSection').style.display = '';
    document.getElementById('dashboardSection').style.display = 'none';
    document.getElementById('navUserArea').style.display = 'none !important';
    document.getElementById('navUserArea').classList.add('d-none');
    document.getElementById('loginForm').reset();
    document.getElementById('registerForm').reset();
}

// ═══════════════════════════════════════════════════════════════
// DASHBOARD
// ═══════════════════════════════════════════════════════════════

function showDashboard() {
    document.getElementById('authSection').style.display = 'none';
    document.getElementById('dashboardSection').style.display = '';

    // Nav user area
    const navArea = document.getElementById('navUserArea');
    navArea.style.display = '';
    navArea.classList.remove('d-none');
    navArea.style.removeProperty('display');
    document.getElementById('navUserName').textContent = currentUser.nome;
    document.getElementById('navUserRole').textContent = currentUser.role === 'Professor' ? '👨‍🏫 Professor' : '🏊 Aluno';

    // Stats
    document.getElementById('statRole').textContent = currentUser.role;

    // Role-based visibility
    const isProfessor = currentUser.role === 'Professor';
    document.querySelectorAll('.professor-only').forEach(el => {
        el.style.display = isProfessor ? '' : 'none';
    });
    document.querySelectorAll('.aluno-only').forEach(el => {
        el.style.display = isProfessor ? 'none' : '';
    });

    if (isProfessor) {
        document.getElementById('reservasTitle').textContent = 'Todas as Reservas';
        document.getElementById('statReservasLabel').textContent = 'Total de Reservas';
    } else {
        document.getElementById('reservasTitle').textContent = 'Minhas Reservas';
        document.getElementById('statReservasLabel').textContent = 'Minhas Reservas';
    }

    // Load data
    loadTurmas();
    loadReservas();
}

// ═══════════════════════════════════════════════════════════════
// TURMAS — CRUD
// ═══════════════════════════════════════════════════════════════

async function loadTurmas() {
    try {
        const res = await apiFetch('/turmas');
        const turmas = await res.json();

        document.getElementById('statTurmas').textContent = turmas.length;

        const tbody = document.getElementById('turmasBody');
        if (turmas.length === 0) {
            tbody.innerHTML = `<tr><td colspan="7" class="text-center py-4 text-secondary">
                <i class="bi bi-water fs-3 d-block mb-2"></i>Nenhuma turma cadastrada.</td></tr>`;
            return;
        }

        tbody.innerHTML = turmas.map(t => {
            const vagasBadge = t.vagasDisponiveis > 0
                ? `<span class="badge-vagas disponivel">${t.vagasDisponiveis}/${t.capacidadeMaxima}</span>`
                : `<span class="badge-vagas lotada">Lotada</span>`;

            let actions = '';
            if (currentUser.role === 'Aluno' && t.vagasDisponiveis > 0) {
                actions = `<button class="btn-action reservar" onclick="criarReserva(${t.id})" title="Reservar vaga">
                    <i class="bi bi-bookmark-plus"></i>
                </button>`;
            } else if (currentUser.role === 'Professor') {
                actions = `
                    <button class="btn-action editar me-1" onclick="editarTurma(${t.id})" title="Editar">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button class="btn-action deletar" onclick="deletarTurma(${t.id}, '${escapeHtml(t.nome)}')" title="Deletar">
                        <i class="bi bi-trash"></i>
                    </button>`;
            }

            return `<tr>
                <td class="ps-4 fw-600">${escapeHtml(t.nome)}</td>
                <td><span class="text-secondary">${escapeHtml(t.modalidade)}</span></td>
                <td>${escapeHtml(t.diaSemana)}</td>
                <td>${t.horarioInicio} - ${t.horarioFim}</td>
                <td>${vagasBadge}</td>
                <td>${escapeHtml(t.professorNome)}</td>
                <td class="text-end pe-4">${actions}</td>
            </tr>`;
        }).join('');
    } catch (err) {
        console.error('Erro ao carregar turmas:', err);
        showToast('Erro ao carregar turmas.', 'danger');
    }
}

async function handleTurmaSubmit(e) {
    e.preventDefault();
    const turmaId = document.getElementById('turmaId').value;
    const isEdit = !!turmaId;

    const payload = {
        nome: document.getElementById('turmaNome').value,
        descricao: document.getElementById('turmaDescricao').value,
        modalidade: document.getElementById('turmaModalidade').value,
        diaSemana: parseInt(document.getElementById('turmaDia').value),
        horarioInicio: document.getElementById('turmaInicio').value,
        horarioFim: document.getElementById('turmaFim').value,
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
        loadTurmas();
    } catch (err) {
        showToast(err.message, 'danger');
    }
}

async function editarTurma(id) {
    try {
        const res = await apiFetch(`/turmas/${id}`);
        const t = await res.json();

        document.getElementById('turmaId').value = t.id;
        document.getElementById('turmaNome').value = t.nome;
        document.getElementById('turmaDescricao').value = t.descricao;
        document.getElementById('turmaModalidade').value = t.modalidade;
        document.getElementById('turmaInicio').value = t.horarioInicio;
        document.getElementById('turmaFim').value = t.horarioFim;
        document.getElementById('turmaCapacidade').value = t.capacidadeMaxima;
        document.getElementById('turmaModalLabel').innerHTML =
            '<i class="bi bi-pencil me-2 text-accent"></i>Editar Turma';

        // Map dia name to number
        const diaMap = { 'Segunda': 1, 'Terca': 2, 'Quarta': 3, 'Quinta': 4, 'Sexta': 5, 'Sabado': 6 };
        document.getElementById('turmaDia').value = diaMap[t.diaSemana] || 1;

        new bootstrap.Modal(document.getElementById('turmaModal')).show();
    } catch (err) {
        showToast('Erro ao carregar turma.', 'danger');
    }
}

async function deletarTurma(id, nome) {
    if (!confirm(`Tem certeza que deseja deletar a turma "${nome}"?`)) return;

    try {
        const res = await apiFetch(`/turmas/${id}`, { method: 'DELETE' });
        if (!res.ok && res.status !== 204) {
            const err = await res.json();
            throw new Error(err.message || 'Erro ao deletar turma.');
        }
        showToast('Turma deletada. 🗑️', 'warning');
        loadTurmas();
        loadReservas();
    } catch (err) {
        showToast(err.message, 'danger');
    }
}

// Reset modal on close
document.getElementById('turmaModal')?.addEventListener('hidden.bs.modal', () => {
    document.getElementById('turmaForm').reset();
    document.getElementById('turmaId').value = '';
    document.getElementById('turmaModalLabel').innerHTML =
        '<i class="bi bi-water me-2 text-accent"></i>Nova Turma';
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
        if (reservas.length === 0) {
            tbody.innerHTML = `<tr><td colspan="6" class="text-center py-4 text-secondary">
                <i class="bi bi-bookmark fs-3 d-block mb-2"></i>Nenhuma reserva encontrada.</td></tr>`;
            return;
        }

        const isProfessor = currentUser.role === 'Professor';

        tbody.innerHTML = reservas.map(r => {
            const statusBadge = r.status === 'Ativa'
                ? '<span class="badge-status ativa">● Ativa</span>'
                : '<span class="badge-status cancelada">● Cancelada</span>';

            const dataFormatted = new Date(r.dataReserva).toLocaleDateString('pt-BR');

            let actions = '';
            if (!isProfessor && r.status === 'Ativa') {
                actions = `<button class="btn-action cancelar" onclick="cancelarReserva(${r.id})" title="Cancelar reserva">
                    <i class="bi bi-x-circle"></i>
                </button>`;
            }

            const alunoCol = isProfessor ? `<td>${escapeHtml(r.alunoNome)}</td>` : '';

            return `<tr>
                <td class="ps-4 fw-600">${escapeHtml(r.turmaNome)}</td>
                ${alunoCol}
                <td>${escapeHtml(r.diaSemana)}</td>
                <td>${r.horarioInicio} - ${r.horarioFim}</td>
                <td>${dataFormatted}</td>
                <td>${statusBadge}</td>
                <td class="text-end pe-4 aluno-only">${actions}</td>
            </tr>`;
        }).join('');

        // Adjust table header for professor (add Aluno column)
        const thead = document.querySelector('#reservasTable thead tr');
        if (isProfessor && !thead.querySelector('.col-aluno')) {
            const th = document.createElement('th');
            th.textContent = 'Aluno';
            th.className = 'col-aluno';
            thead.insertBefore(th, thead.children[1]);
        }
    } catch (err) {
        console.error('Erro ao carregar reservas:', err);
        showToast('Erro ao carregar reservas.', 'danger');
    }
}

async function criarReserva(turmaId) {
    if (!confirm('Deseja reservar uma vaga nesta turma?')) return;

    try {
        const res = await apiFetch('/reservas', {
            method: 'POST',
            body: JSON.stringify({ turmaId })
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Erro ao criar reserva.');
        }

        showToast('Reserva realizada com sucesso! 🎉', 'success');
        loadTurmas();
        loadReservas();
    } catch (err) {
        showToast(err.message, 'danger');
    }
}

async function cancelarReserva(id) {
    if (!confirm('Tem certeza que deseja cancelar esta reserva?')) return;

    try {
        const res = await apiFetch(`/reservas/${id}`, { method: 'DELETE' });
        if (!res.ok && res.status !== 204) {
            const err = await res.json();
            throw new Error(err.message || 'Erro ao cancelar reserva.');
        }

        showToast('Reserva cancelada. 🗑️', 'warning');
        loadTurmas();
        loadReservas();
    } catch (err) {
        showToast(err.message, 'danger');
    }
}

// ═══════════════════════════════════════════════════════════════
// UTILIDADES
// ═══════════════════════════════════════════════════════════════

/**
 * Wrapper para fetch com autenticação JWT automática.
 * Redireciona para login se o token expirar (401).
 */
async function apiFetch(path, options = {}) {
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers
    };

    if (currentUser?.token) {
        headers['Authorization'] = `Bearer ${currentUser.token}`;
    }

    const res = await fetch(`${API_BASE}${path}`, { ...options, headers });

    // Token expirado ou inválido
    if (res.status === 401) {
        showToast('Sessão expirada. Faça login novamente.', 'warning');
        logout();
        throw new Error('Sessão expirada.');
    }

    if (res.status === 403) {
        showToast('Você não tem permissão para esta ação.', 'danger');
        throw new Error('Permissão negada.');
    }

    return res;
}

function showAuthAlert(message, type) {
    const alert = document.getElementById('authAlert');
    alert.style.display = '';
    alert.innerHTML = `<div class="alert alert-${type} alert-dismissible fade show rounded-pill py-2 px-3" role="alert">
        <small>${escapeHtml(message)}</small>
        <button type="button" class="btn-close btn-close-white btn-sm" data-bs-dismiss="alert"></button>
    </div>`;
}

function showToast(message, type = 'success') {
    const toast = document.getElementById('appToast');
    toast.className = `toast align-items-center border-0 bg-${type}-toast text-white`;
    document.getElementById('toastMessage').textContent = message;
    bootstrap.Toast.getOrCreateInstance(toast, { delay: 4000 }).show();
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
