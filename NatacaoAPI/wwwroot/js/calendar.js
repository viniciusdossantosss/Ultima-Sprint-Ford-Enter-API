// ═══════════════════════════════════════════════════════════════
// CALENDAR MODULE
// ═══════════════════════════════════════════════════════════════

import { apiFetch, getCurrentUser } from './api.js';
import { showToast, escapeHtml, getProfessorSpecialties } from './utils.js';
import { loadReservas, criarReserva } from './reservas.js';
import { openNivelModalFromEvent } from './users.js';

export let calendarInstance = null;

export function initCalendar() {
    const calendarEl = document.getElementById('calendar');
    if (!calendarEl) return;
    const isCypress = typeof window !== 'undefined' && (window.Cypress || window.cypress);
    
    // Utilizando o FullCalendar injetado globalmente na página
    calendarInstance = new window.FullCalendar.Calendar(calendarEl, {
        locale: 'pt-br',
        initialView: window.innerWidth < 768 ? 'listWeek' : 'dayGridMonth',
        customButtons: {
            todayCustom: {
                text: 'Hoje',
                click: function() {
                    if (calendarInstance) {
                        calendarInstance.today();
                    }
                }
            }
        },
        headerToolbar: { left: 'prev,next todayCustom', center: 'title', right: 'dayGridMonth,timeGridWeek,listWeek' },
        buttonText: {
            month: 'Mês',
            week: 'Semana',
            day: 'Dia',
            list: 'Lista'
        },
        height: 'auto',
        events: fetchTurmas,
        eventClick: handleEventClick,
        dateClick: handleDateClick,
        dayMaxEvents: isCypress ? false : 3,
        moreLinkText: function(n) { return '+' + n + ' turmas'; }
    });
    calendarInstance.render();
}

export async function fetchTurmas(info, successCallback) {
    try {
        const res = await apiFetch('/turmas');
        const turmas = await res.json();
        
        const statTurmas = document.getElementById('statTurmas');
        if (statTurmas) statTurmas.textContent = turmas.length;

        const events = turmas.map(t => {
            let title = `${t.nome} (${t.vagasDisponiveis}/${t.capacidadeMaxima})`;
            if (t.professorCertificacaoExpirada) {
                title = `⚠️ [INCONFORME] ${title}`;
            }
            return {
                id: t.id,
                title: title,
                start: t.dataHoraInicio,
                end: t.dataHoraFim,
                className: t.vagasDisponiveis > 0 ? 'event-vagas' : 'event-lotada',
                extendedProps: t
            };
        });
        successCallback(events);
    } catch (err) {
        console.error('Erro ao carregar turmas:', err);
        successCallback([]);
    }
}

export function handleEventClick(info) {
    const t = info.event.extendedProps;
    const inicio = new Date(t.dataHoraInicio).toLocaleString('pt-BR');
    const fim = new Date(t.dataHoraFim).toLocaleString('pt-BR');
    const vagasBadge = t.vagasDisponiveis > 0
        ? `<span class="badge-vagas disponivel">${t.vagasDisponiveis}/${t.capacidadeMaxima} vagas</span>`
        : `<span class="badge-vagas lotada">Lotada</span>`;

    let warningCerts = '';
    if (t.professorCertificacaoExpirada) {
        warningCerts = `
            <div class="alert alert-danger d-flex align-items-center gap-2 mt-3 mb-0 py-2 border-0" style="background: rgba(239, 68, 68, 0.1); color: #fca5a5; font-size: 0.85rem; border-radius: 8px;">
                <i class="bi bi-exclamation-triangle-fill fs-5"></i>
                <div><strong>Escala Irregular:</strong> ${escapeHtml(t.professorInconformidadeMensagem || '')}</div>
            </div>`;
    }

    let inscritosHtml = '';
    const currentUser = getCurrentUser();
    const isProfOrAdmin = currentUser.role === 'Professor' || currentUser.role === 'Admin';
    if (isProfOrAdmin) {
        if (t.alunosInscritos && t.alunosInscritos.length > 0) {
            const listaAlunos = t.alunosInscritos.map(aluno => {
                return `
                    <div class="d-flex align-items-center justify-content-between py-1 border-bottom border-secondary-subtle" style="font-size: 0.85rem;">
                        <div>
                            <span class="fw-500">${escapeHtml(aluno.nome)}</span>
                            <span class="badge bg-secondary-subtle text-light ms-2" style="font-size: 0.7rem;">${escapeHtml(aluno.nivelPedagogico || 'Iniciante')}</span>
                        </div>
                        <button class="btn btn-link text-accent p-0 m-0" style="font-size: 0.8rem; text-decoration: none;" onclick="openNivelModalFromEvent(${aluno.id}, '${escapeHtml(aluno.nivelPedagogico || 'Iniciante')}')">
                            <i class="bi bi-award"></i> Alterar
                        </button>
                    </div>
                `;
            }).join('');
            
            inscritosHtml = `
                <div class="mt-4 pt-3 border-top border-secondary-subtle">
                    <h6 class="fw-700 text-accent mb-2"><i class="bi bi-people me-1"></i>Alunos Inscritos (${t.alunosInscritos.length})</h6>
                    <div style="max-height: 150px; overflow-y: auto; padding-right: 5px;">
                        ${listaAlunos}
                    </div>
                </div>
            `;
        } else {
            inscritosHtml = `
                <div class="mt-4 pt-3 border-top border-secondary-subtle">
                    <h6 class="fw-700 text-accent mb-1"><i class="bi bi-people me-1"></i>Alunos Inscritos (0)</h6>
                    <div class="text-secondary" style="font-size: 0.85rem;">Nenhum aluno inscrito nesta turma ainda.</div>
                </div>
            `;
        }
    }

    const specialties = getProfessorSpecialties(t.professorNome, t.modalidade);
    const specialtiesHtml = specialties ? `<div class="text-secondary" style="font-size: 0.85rem; padding-left: 24px; margin-top: -4px;"><i class="bi bi-award me-1 text-muted"></i>Especialidades: ${escapeHtml(specialties)}</div>` : '';

    const modalBody = document.getElementById('eventoModalBody');
    if (modalBody) {
        modalBody.innerHTML = `
            <h5 class="fw-700 mb-2">${escapeHtml(t.nome)}</h5>
            <p class="text-secondary mb-3">${escapeHtml(t.descricao || '')}</p>
            <div class="d-flex flex-column gap-2">
                <div><i class="bi bi-tag me-2 text-accent"></i>${escapeHtml(t.modalidade)}</div>
                <div><i class="bi bi-clock me-2 text-accent"></i>${inicio} → ${fim}</div>
                <div>
                    <i class="bi bi-person me-2 text-accent"></i>${escapeHtml(t.professorNome)}
                    ${specialtiesHtml}
                </div>
                <div><i class="bi bi-people me-2 text-accent"></i>${vagasBadge}</div>
            </div>
            ${warningCerts}
            ${inscritosHtml}`;
    }

    const modalFooter = document.getElementById('eventoModalFooter');
    if (modalFooter) {
        modalFooter.innerHTML = '';

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
    }

    const titleEl = document.getElementById('eventoModalTitle');
    if (titleEl) titleEl.textContent = t.nome;
    
    if (window.bootstrap) {
        new window.bootstrap.Modal(document.getElementById('eventoModal')).show();
    }
}

export function handleDateClick(info) {
    const currentUser = getCurrentUser();
    if (currentUser.role !== 'Professor' && currentUser.role !== 'Admin') return;
    
    document.getElementById('turmaId').value = '';
    const form = document.getElementById('turmaForm');
    if (form) form.reset();
    
    document.getElementById('turmaInicio').value = info.dateStr + 'T08:00';
    document.getElementById('turmaFim').value = info.dateStr + 'T09:00';
    document.getElementById('turmaModalLabel').innerHTML = '<i class="bi bi-water me-2 text-accent"></i>Nova Turma';
    
    if (currentUser.role === 'Admin') {
        populateProfessorsDropdown();
    }
    
    if (window.bootstrap) {
        new window.bootstrap.Modal(document.getElementById('turmaModal')).show();
    }
}

export async function handleTurmaSubmit(e) {
    e.preventDefault();
    const btn = e.target.querySelector('button[type="submit"]');
    if (btn) btn.disabled = true;
    const turmaId = document.getElementById('turmaId').value;
    const isEdit = !!turmaId;

    const currentUser = getCurrentUser();
    const payload = {
        nome: document.getElementById('turmaNome').value,
        descricao: document.getElementById('turmaDescricao').value,
        modalidade: document.getElementById('turmaModalidade').value,
        dataHoraInicio: document.getElementById('turmaInicio').value,
        dataHoraFim: document.getElementById('turmaFim').value,
        capacidadeMaxima: parseInt(document.getElementById('turmaCapacidade').value)
    };

    if (currentUser.role === 'Admin') {
        const profSelect = document.getElementById('turmaProfessorId');
        payload.professorId = profSelect.value ? parseInt(profSelect.value) : null;
    }

    try {
        const url = isEdit ? `/turmas/${turmaId}` : '/turmas';
        const method = isEdit ? 'PUT' : 'POST';
        const res = await apiFetch(url, { method, body: JSON.stringify(payload) });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Erro ao salvar turma.');
        }

        if (window.bootstrap) {
            window.bootstrap.Modal.getInstance(document.getElementById('turmaModal')).hide();
        }
        showToast(isEdit ? 'Turma atualizada! ✅' : 'Turma criada! ✅', 'success');
        if (calendarInstance) calendarInstance.refetchEvents();
        loadReservas();
    } catch (err) {
        showToast(err.message, 'danger');
    } finally {
        if (btn) btn.disabled = false;
    }
}

export async function editarTurma(id) {
    const currentUser = getCurrentUser();
    try {
        if (window.bootstrap) {
            window.bootstrap.Modal.getInstance(document.getElementById('eventoModal'))?.hide();
        }
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

        if (currentUser.role === 'Admin') {
            await populateProfessorsDropdown();
            document.getElementById('turmaProfessorId').value = t.professorId || '';
        }

        if (window.bootstrap) {
            new window.bootstrap.Modal(document.getElementById('turmaModal')).show();
        }
    } catch (err) {
        showToast('Erro ao carregar turma.', 'danger');
    }
}

export async function deletarTurma(id, nome) {
    if (window.bootstrap) {
        window.bootstrap.Modal.getInstance(document.getElementById('eventoModal'))?.hide();
    }
    if (!confirm(`Tem certeza que deseja deletar "${nome}"?`)) return;

    try {
        const res = await apiFetch(`/turmas/${id}`, { method: 'DELETE' });
        if (!res.ok && res.status !== 204) throw new Error('Erro ao deletar.');
        showToast('Turma deletada. 🗑️', 'warning');
        if (calendarInstance) calendarInstance.refetchEvents();
        loadReservas();
    } catch (err) { showToast(err.message, 'danger'); }
}

export async function populateProfessorsDropdown() {
    const select = document.getElementById('turmaProfessorId');
    if (!select) return;
    
    select.innerHTML = '<option value="">Selecione um Professor...</option>';
    
    try {
        const res = await apiFetch('/usuarios');
        const users = await res.json();
        const profs = users.filter(u => u.role === 'Professor');
        
        profs.forEach(p => {
            const opt = document.createElement('option');
            opt.value = p.id;
            opt.textContent = `${p.nome} (CREF: ${p.cref || 'N/A'}${p.crefAtivo ? ' - Ativo' : ' - Inativo'})`;
            select.appendChild(opt);
        });
    } catch (err) {
        console.error('Erro ao carregar dropdown de professores:', err);
    }
}

// Event listener para resetar modal de turmas quando fechado
document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('turmaModal');
    if (modal) {
        modal.addEventListener('hidden.bs.modal', () => {
            const form = document.getElementById('turmaForm');
            if (form) form.reset();
            document.getElementById('turmaId').value = '';
            document.getElementById('turmaModalLabel').innerHTML = '<i class="bi bi-water me-2 text-accent"></i>Nova Turma';
        });
    }
});
