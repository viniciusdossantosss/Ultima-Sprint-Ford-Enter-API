// ═══════════════════════════════════════════════════════════════
// RESERVAS MODULE
// ═══════════════════════════════════════════════════════════════

import { apiFetch, getCurrentUser } from './api.js';
import { showToast, escapeHtml, getTableSkeletonHtml } from './utils.js';
import { calendarInstance } from './calendar.js';

export async function loadReservas() {
    const tbody = document.getElementById('reservasBody');
    const currentUser = getCurrentUser();
    const isProfOrAdmin = currentUser && (currentUser.role === 'Professor' || currentUser.role === 'Admin');

    // Sincronizar colunas antes de carregar o esqueleto
    const thead = document.querySelector('#reservasTable thead tr');
    if (thead) {
        const hasAlunoCol = thead.querySelector('.col-aluno');
        if (isProfOrAdmin && !hasAlunoCol) {
            const th = document.createElement('th');
            th.textContent = 'Aluno';
            th.className = 'col-aluno';
            thead.insertBefore(th, thead.children[1]);
        } else if (!isProfOrAdmin && hasAlunoCol) {
            hasAlunoCol.remove();
        }
    }

    if (tbody) {
        tbody.innerHTML = getTableSkeletonHtml(isProfOrAdmin ? 6 : 5, 3);
    }

    try {
        const res = await apiFetch('/reservas');
        const reservas = await res.json();

        const ativas = reservas.filter(r => r.status === 'Ativa');
        const statReservas = document.getElementById('statReservas');
        if (statReservas) statReservas.textContent = ativas.length;

        if (!tbody) return;

        if (reservas.length === 0) {
            tbody.innerHTML = `<tr>
                <td colspan="${isProfOrAdmin ? 6 : 5}" class="text-center p-0">
                    <div class="empty-state-container">
                        <div class="empty-state-card">
                            <div class="empty-state-icon">
                                <i class="bi bi-calendar-x"></i>
                            </div>
                            <div class="empty-state-title">Nenhuma reserva encontrada</div>
                            <div class="empty-state-desc">Você ainda não possui reservas ou nenhuma reserva corresponde aos filtros.</div>
                        </div>
                    </div>
                </td>
            </tr>`;
            return;
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
    } catch (err) { 
        showToast('Erro ao carregar reservas.', 'danger'); 
    }
}

export async function criarReserva(turmaId) {
    if (window.bootstrap) {
        window.bootstrap.Modal.getInstance(document.getElementById('eventoModal'))?.hide();
    }
    if (!confirm('Deseja reservar uma vaga nesta turma?')) return;

    try {
        const res = await apiFetch('/reservas', { method: 'POST', body: JSON.stringify({ turmaId }) });
        if (!res.ok) { 
            const err = await res.json(); 
            throw new Error(err.message); 
        }
        showToast('Reserva realizada! 🎉', 'success');
        if (calendarInstance) calendarInstance.refetchEvents();
        loadReservas();
    } catch (err) { 
        showToast(err.message, 'danger'); 
    }
}

export async function cancelarReserva(id) {
    if (!confirm('Cancelar esta reserva?')) return;
    try {
        const res = await apiFetch(`/reservas/${id}`, { method: 'DELETE' });
        if (!res.ok && res.status !== 204) throw new Error('Erro ao cancelar.');
        showToast('Reserva cancelada. 🗑️', 'warning');
        if (calendarInstance) calendarInstance.refetchEvents();
        loadReservas();
    } catch (err) { 
        showToast(err.message, 'danger'); 
    }
}
