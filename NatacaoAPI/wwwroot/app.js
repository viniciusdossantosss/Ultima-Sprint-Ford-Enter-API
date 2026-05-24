// ═══════════════════════════════════════════════════════════════
// AquaSchedule — Frontend Application (Multi-page with FullCalendar)
// ═══════════════════════════════════════════════════════════════

const API_BASE = '/api';

// ─── Estado da Aplicação ──────────────────────────────────────
let currentUser = null;
let calendar = null; // Objeto do FullCalendar

// ─── Inicialização Baseada na Página ──────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    const saved = localStorage.getItem('aquaUser');
    if (saved) {
        currentUser = JSON.parse(saved);
    }

    const currentPath = window.location.pathname;

    if (currentPath.endsWith('/') || currentPath.endsWith('index.html')) {
        if (currentUser) {
            window.location.href = 'dashboard.html';
        }
    } 
    else if (currentPath.endsWith('dashboard.html')) {
        if (!currentUser) {
            window.location.href = 'index.html';
            return;
        }
        initDashboard();
    }
});

// ═══════════════════════════════════════════════════════════════
// AUTENTICAÇÃO (Apenas na index.html)
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
        window.location.href = 'dashboard.html';

    } catch (err) {
        showAuthAlert(err.message, 'danger');
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
        window.location.href = 'dashboard.html';

    } catch (err) {
        showAuthAlert(err.message, 'danger');
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-person-plus me-2"></i>Criar Conta';
    }
}

function logout() {
    currentUser = null;
    localStorage.removeItem('aquaUser');
    window.location.href = 'index.html';
}

// ═══════════════════════════════════════════════════════════════
// DASHBOARD (Apenas na dashboard.html)
// ═══════════════════════════════════════════════════════════════

function initDashboard() {
    document.getElementById('navUserName').textContent = currentUser.nome;
    document.getElementById('navUserRole').textContent = currentUser.role === 'Professor' ? '👨‍🏫 Professor' : '🏊 Aluno';
    document.getElementById('statRole').textContent = currentUser.role;

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

    initCalendar();
    loadReservas();
}

// ═══════════════════════════════════════════════════════════════
// CALENDÁRIO (FullCalendar)
// ═══════════════════════════════════════════════════════════════

function initCalendar() {
    const calendarEl = document.getElementById('calendar');
    calendar = new FullCalendar.Calendar(calendarEl, {
        locale: 'pt-br',
        initialView: 'timeGridWeek',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay'
        },
        slotMinTime: '06:00:00',
        slotMaxTime: '22:00:00',
        allDaySlot: false,
        events: fetchTurmas,
        eventClick: handleEventClick,
        dateClick: handleDateClick
    });
    calendar.render();
}

async function fetchTurmas(fetchInfo, successCallback, failureCallback) {
    try {
        const res = await apiFetch('/turmas');
        const turmas = await res.json();
        document.getElementById('statTurmas').textContent = turmas.length;

        const events = turmas.map(t => ({
            id: t.id,
            title: t.nome,
            start: t.dataHoraInicio,
            end: t.dataHoraFim,
            extendedProps: {
                ...t
            },
            // Estilo do evento
            className: t.vagasDisponiveis > 0 ? 'event-vagas' : 'event-lotada',
        }));
        successCallback(events);
    } catch (err) {
        console.error('Erro ao carregar turmas:', err);
        showToast('Erro ao carregar turmas.', 'danger');
        failureCallback(err);
    }
}

function handleEventClick(info) {
    const t = info.event.extendedProps;
    const modalTitle = document.getElementById('eventoModalTitle');
    const modalBody = document.getElementById('eventoModalBody');
    const modalFooter = document.getElementById('eventoModalFooter');

    modalTitle.textContent = t.nome;
    modalBody.innerHTML = `
        <p><strong>Modalidade:</strong> ${escapeHtml(t.modalidade)}</p>
        <p><strong>Professor:</strong> ${escapeHtml(t.professorNome)}</p>
        <p><strong>Horário:</strong> ${new Date(t.dataHoraInicio).toLocaleString('pt-BR')} - ${new Date(t.dataHoraFim).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}</p>
        <p><strong>Vagas:</strong> ${t.vagasDisponiveis} / ${t.capacidadeMaxima}</p>
        <p class="mt-3">${escapeHtml(t.descricao)}</p>
    `;

    modalFooter.innerHTML = ''; // Limpa botões anteriores
    if (currentUser.role === 'Professor') {
        modalFooter.innerHTML = `
            <button class="btn btn-outline-warning rounded-pill" onclick="editarTurma(${t.id})">Editar</button>
            <button class="btn btn-outline-danger rounded-pill" onclick="deletarTurma(${t.id}, '${escapeHtml(t.nome)}')">Deletar</button>
        `;
    } else if (t.vagasDisponiveis > 0) {
        modalFooter.innerHTML = `<button class="btn btn-accent rounded-pill" onclick="criarReserva(${t.id})">Reservar Vaga</button>`;
    }

    new bootstrap.Modal(document.getElementById('eventoModal')).show();
}

function handleDateClick(info) {
    if (currentUser.role !== 'Professor') return;

    // Abre o modal de criação de turma, pré-preenchendo a data/hora
    document.getElementById('turmaForm').reset();
    document.getElementById('turmaId').value = '';
    document.getElementById('turmaModalLabel').innerHTML = '<i class="bi bi-water me-2 text-accent"></i>Nova Turma';
    
    // Formata a data para o input datetime-local (YYYY-MM-DDTHH:mm)
    // Subtrai o offset do fuso horário para manter a hora local correta
    const offset = info.date.getTimezoneOffset() * 60000;
    const localDate = new Date(info.date.getTime() - offset);
    
    const dataInicioStr = localDate.toISOString().substring(0, 16);
    
    // Fim é início + 1 hora
    localDate.setHours(localDate.getHours() + 1);
    const dataFimStr = localDate.toISOString().substring(0, 16);

    document.getElementById('turmaInicio').value = dataInicioStr;
    document.getElementById('turmaFim').value = dataFimStr;

    new bootstrap.Modal(document.getElementById('turmaModal')).show();
}

// ═══════════════════════════════════════════════════════════════
// TURMAS — CRUD (agora via Modals)
// ═══════════════════════════════════════════════════════════════

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
        calendar.refetchEvents(); // Recarrega os eventos no calendário
        loadReservas(); // Atualiza a lista de reservas
    } catch (err) {
        showToast(err.message, 'danger');
    }
}

async function editarTurma(id) {
    try {
        // Fecha o modal de detalhes primeiro
        const eventoModal = bootstrap.Modal.getInstance(document.getElementById('eventoModal'));
        if (eventoModal) eventoModal.hide();

        const res = await apiFetch(`/turmas/${id}`);
        const t = await res.json();

        document.getElementById('turmaId').value = t.id;
        document.getElementById('turmaNome').value = t.nome;
        document.getElementById('turmaDescricao').value = t.descricao;
        document.getElementById('turmaModalidade').value = t.modalidade;
        document.getElementById('turmaCapacidade').value = t.capacidadeMaxima;
        document.getElementById('turmaModalLabel').innerHTML = '<i class="bi bi-pencil me-2 text-accent"></i>Editar Turma';

        // Formata as datas para o input datetime-local, ajustando o fuso horário
        const inicioDate = new Date(t.dataHoraInicio);
        const inicioOffset = inicioDate.getTimezoneOffset() * 60000;
        document.getElementById('turmaInicio').value = new Date(inicioDate.getTime() - inicioOffset).toISOString().substring(0, 16);

        const fimDate = new Date(t.dataHoraFim);
        const fimOffset = fimDate.getTimezoneOffset() * 60000;
        document.getElementById('turmaFim').value = new Date(fimDate.getTime() - fimOffset).toISOString().substring(0, 16);

        new bootstrap.Modal(document.getElementById('turmaModal')).show();
    } catch (err) {
        showToast('Erro ao carregar turma para edição.', 'danger');
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
        
        const eventoModal = bootstrap.Modal.getInstance(document.getElementById('eventoModal'));
        if (eventoModal) eventoModal.hide();

        showToast('Turma deletada. 🗑️', 'warning');
        calendar.refetchEvents();
        loadReservas();
    } catch (err) {
        showToast(err.message, 'danger');
    }
}

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
            tbody.innerHTML = `<tr><td colspan="5" class="text-center py-4 text-secondary">
                <i class="bi bi-bookmark fs-3 d-block mb-2"></i>Nenhuma reserva encontrada.</td></tr>`;
            return;
        }

        const isProfessor = currentUser.role === 'Professor';

        // Previne header duplicado
        const thead = document.querySelector('#reservasTable thead tr');
        const existingCol = thead.querySelector('.col-aluno');
        if (isProfessor) {
             if (!existingCol) {
                 const th = document.createElement('th');
                 th.textContent = 'Aluno';
                 th.className = 'col-aluno';
                 thead.insertBefore(th, thead.children[1]);
             }
        } else {
             if(existingCol) existingCol.remove();
        }

        tbody.innerHTML = reservas.map(r => {
            const statusBadge = r.status === 'Ativa'
                ? '<span class="badge-status ativa">● Ativa</span>'
                : '<span class="badge-status cancelada">● Cancelada</span>';

            const dataReservaFormatted = new Date(r.dataReserva).toLocaleDateString('pt-BR');
            const dataAulaFormatted = new Date(r.dataHoraInicio).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });

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
                <td>${dataAulaFormatted}</td>
                <td>${dataReservaFormatted}</td>
                <td>${statusBadge}</td>
                <td class="text-end pe-4 aluno-only">${actions}</td>
            </tr>`;
        }).join('');
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

        const eventoModal = bootstrap.Modal.getInstance(document.getElementById('eventoModal'));
        if (eventoModal) eventoModal.hide();

        showToast('Reserva realizada com sucesso! 🎉', 'success');
        calendar.refetchEvents();
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
        calendar.refetchEvents();
        loadReservas();
    } catch (err) {
        showToast(err.message, 'danger');
    }
}

// ═══════════════════════════════════════════════════════════════
// UTILIDADES
// ═══════════════════════════════════════════════════════════════

async function apiFetch(path, options = {}) {
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers
    };

    if (currentUser?.token) {
        headers['Authorization'] = `Bearer ${currentUser.token}`;
    }

    const res = await fetch(`${API_BASE}${path}`, { ...options, headers });

    if (res.status === 401) {
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
    if(alert) {
        alert.style.display = '';
        alert.innerHTML = `<div class="alert alert-${type} alert-dismissible fade show rounded-pill py-2 px-3" role="alert">
            <small>${escapeHtml(message)}</small>
            <button type="button" class="btn-close btn-close-white btn-sm" data-bs-dismiss="alert"></button>
        </div>`;
    }
}

function showToast(message, type = 'success') {
    const toast = document.getElementById('appToast');
    if (toast) {
        toast.className = `toast align-items-center border-0 bg-${type}-toast text-white`;
        document.getElementById('toastMessage').textContent = message;
        bootstrap.Toast.getOrCreateInstance(toast, { delay: 4000 }).show();
    }
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}