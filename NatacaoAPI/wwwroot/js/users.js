// ═══════════════════════════════════════════════════════════════
// USERS MODULE
// ═══════════════════════════════════════════════════════════════

import { apiFetch, getCurrentUser } from './api.js';
import { showToast, escapeHtml, getTableSkeletonHtml, calcularIdade } from './utils.js';
import { calendarInstance } from './calendar.js';

export let cachedUsuarios = [];
export let editingUsuarioId = null;

export async function loadUsuarios() {
    const tbody = document.getElementById('usuariosBody');
    if (tbody) {
        tbody.innerHTML = getTableSkeletonHtml(7, 3);
    }
    const currentUser = getCurrentUser();
    try {
        const url = currentUser.role === 'Professor' ? '/usuarios/alunos' : '/usuarios';
        const res = await apiFetch(url);
        cachedUsuarios = await res.json();

        const alunos = cachedUsuarios.filter(u => u.role === 'Aluno');
        const profs = cachedUsuarios.filter(u => u.role === 'Professor');
        const elAlunos = document.getElementById('statAlunos');
        const elProfs = document.getElementById('statProfessores');
        if (elAlunos) elAlunos.textContent = alunos.length;
        if (elProfs) elProfs.textContent = profs.length;

        // Limpar campo de busca ao recarregar a lista geral
        const buscaInput = document.getElementById('buscaUsuario');
        if (buscaInput) buscaInput.value = '';

        if (currentUser.role === 'Professor') {
            renderUsuariosTable(cachedUsuarios.filter(u => u.role === 'Aluno'));
        } else {
            renderUsuariosTable(cachedUsuarios);
        }
    } catch (err) { 
        showToast('Erro ao carregar usuários.', 'danger'); 
    }
}

export function renderUsuariosTable(usuariosList) {
    const tbody = document.getElementById('usuariosBody');
    if (!tbody) return;

    if (usuariosList.length === 0) {
        tbody.innerHTML = `<tr>
            <td colspan="7" class="text-center p-0">
                <div class="empty-state-container">
                    <div class="empty-state-card">
                        <div class="empty-state-icon">
                            <i class="bi bi-people"></i>
                        </div>
                        <div class="empty-state-title">Nenhum usuário encontrado</div>
                        <div class="empty-state-desc">Não há registros correspondentes a esta busca ou nenhum usuário foi cadastrado.</div>
                    </div>
                </div>
            </td>
        </tr>`;
        return;
    }

    const currentUser = getCurrentUser();
    tbody.innerHTML = usuariosList.map(u => {
        const roleBadge = `<span class="badge-role ${u.role.toLowerCase()}">${u.role}</span>`;
        const data = new Date(u.dataCriacao).toLocaleDateString('pt-BR');
        
        let actions = '';
        if (currentUser.role === 'Professor') {
            actions = `
                <button class="btn btn-outline-info btn-xs rounded-pill px-2 py-0" style="font-size: 0.75rem;" onclick="openNivelModal(${u.id}, '${escapeHtml(u.nivelPedagogico || 'Iniciante')}')" title="Alterar Nível">
                    <i class="bi bi-award me-1"></i>Nível
                </button>
            `;
        } else if (u.role !== 'Admin') {
            actions = `
                <button class="btn btn-outline-info btn-xs rounded-pill px-2 py-0 me-1" style="font-size: 0.75rem;" onclick="openNivelModal(${u.id}, '${escapeHtml(u.nivelPedagogico || 'Iniciante')}')" title="Alterar Nível">
                    <i class="bi bi-award"></i>
                </button>
                <button class="btn-action editar" onclick="editUsuario(${u.id})" title="Editar">
                    <i class="bi bi-pencil"></i>
                </button>
                <button class="btn-action deletar" onclick="deleteUsuario(${u.id}, '${escapeHtml(u.nome)}')" title="Excluir">
                    <i class="bi bi-trash"></i>
                </button>
            `;
        } else {
            actions = '<span class="text-muted" title="Admin não pode ser modificado"><i class="bi bi-shield-lock"></i></span>';
        }

        let emailTelefone = `<div>${escapeHtml(u.email)}</div>`;
        if (u.telefone) {
            emailTelefone += `<div class="text-secondary" style="font-size: 0.8rem;"><i class="bi bi-telephone me-1"></i>${escapeHtml(u.telefone)}</div>`;
        }

        // Coluna Detalhes Aluno/Prof
        let detalhes = '<span class="text-muted">-</span>';
        if (u.role === 'Aluno') {
            const idadeStr = u.dataNascimento ? `${calcularIdade(u.dataNascimento)} anos` : '';
            detalhes = `<div><span class="badge bg-secondary-subtle text-light">${u.nivelPedagogico || 'Iniciante'}</span></div>`;
            if (u.modalidadeSugerida) {
                detalhes += `<div class="text-secondary" style="font-size: 0.8rem;"><i class="bi bi-tag me-1"></i>Sugerido: ${escapeHtml(u.modalidadeSugerida)} (${idadeStr})</div>`;
            }
            if (u.nomeResponsavel) {
                detalhes += `<div class="text-info mt-1" style="font-size: 0.75rem; line-height: 1.1;" title="Telefone do Responsável: ${u.telefoneResponsavel}">
                    <i class="bi bi-shield-fill-check me-1"></i>Resp: ${escapeHtml(u.nomeResponsavel)} (${escapeHtml(u.telefoneResponsavel)})
                </div>`;
            }
        } else if (u.role === 'Professor') {
            const crefStatus = u.crefAtivo
                ? '<span class="badge bg-success-subtle text-success" style="font-size: 0.75rem; padding: 2px 6px; border-radius: 4px;"><i class="bi bi-check-circle-fill me-1"></i>CREF Ativo</span>'
                : '<span class="badge bg-danger-subtle text-danger" style="font-size: 0.75rem; padding: 2px 6px; border-radius: 4px;"><i class="bi bi-x-circle-fill me-1"></i>CREF Inativo</span>';
            detalhes = `<div><span class="fw-semibold text-secondary">CREF:</span> ${escapeHtml(u.cref || 'Não informado')}</div>
                        <div class="mt-1">${crefStatus}</div>`;
        }

        // Coluna Saúde / CREF / Doc
        let saudeDoc = '<span class="text-muted">-</span>';
        if (u.role === 'Aluno') {
            const badgeDoc = u.documentacaoSaudeEntregue
                ? '<span class="badge bg-success-subtle text-success" style="font-size: 0.75rem;"><i class="bi bi-check-circle me-1"></i>Doc. OK</span>'
                : '<span class="badge bg-warning-subtle text-warning" style="font-size: 0.75rem;"><i class="bi bi-exclamation-circle me-1"></i>Doc. Pendente</span>';
            
            const badgeProblema = u.problemasSaude
                ? `<div class="text-danger mt-1" style="font-size: 0.75rem; line-height: 1.1;" title="${escapeHtml(u.problemasSaude)}"><i class="bi bi-heart-pulse me-1"></i>Restrição: ${escapeHtml(u.problemasSaude)}</div>`
                : '<div class="text-success mt-1" style="font-size: 0.75rem;"><i class="bi bi-heart-fill me-1"></i>Sem restrições</div>';
            
            saudeDoc = `<div>${badgeDoc}</div>${badgeProblema}`;
        } else if (u.role === 'Professor') {
            const hoje = new Date();
            hoje.setHours(0,0,0,0);
            
            let salvHtml = '<div class="text-secondary" style="font-size: 0.8rem;"><i class="bi bi-shield-fill-exclamation me-1 text-danger"></i>Salvamento: N/C</div>';
            if (u.validadeSalvamentoAquatico) {
                const valSal = new Date(u.validadeSalvamentoAquatico);
                valSal.setHours(0,0,0,0);
                const isExpirado = valSal < hoje;
                salvHtml = `<div style="font-size: 0.8rem; color: ${isExpirado ? '#fca5a5' : '#aaa'}"><i class="bi ${isExpirado ? 'bi-exclamation-triangle-fill text-danger' : 'bi-shield-check text-success'} me-1"></i>Salvamento: ${valSal.toLocaleDateString('pt-BR')}</div>`;
            }
            
            let primHtml = '<div class="text-secondary" style="font-size: 0.8rem;"><i class="bi bi-heart-pulse-fill me-1 text-danger"></i>1º Socorros: N/C</div>';
            if (u.validadePrimeirosSocorros) {
                const valPri = new Date(u.validadePrimeirosSocorros);
                valPri.setHours(0,0,0,0);
                const isExpirado = valPri < hoje;
                primHtml = `<div style="font-size: 0.8rem; color: ${isExpirado ? '#fca5a5' : '#aaa'}"><i class="bi ${isExpirado ? 'bi-exclamation-triangle-fill text-danger' : 'bi-heart-fill text-success'} me-1"></i>1º Soc.: ${valPri.toLocaleDateString('pt-BR')}</div>`;
            }
            
            saudeDoc = `<div>${salvHtml}</div><div class="mt-1">${primHtml}</div>`;
        }

        return `<tr>
            <td class="fw-600">${escapeHtml(u.nome)}</td>
            <td>${emailTelefone}</td>
            <td>${roleBadge}</td>
            <td>${detalhes}</td>
            <td>${saudeDoc}</td>
            <td>${data}</td>
            <td class="text-end">${actions}</td>
        </tr>`;
    }).join('');
}

export function filtrarUsuarios() {
    const query = document.getElementById('buscaUsuario').value.toLowerCase().trim();
    const filtered = cachedUsuarios.filter(u => u.nome.toLowerCase().includes(query));
    renderUsuariosTable(filtered);
}

export async function editUsuario(id) {
    editingUsuarioId = id;
    try {
        const res = await apiFetch(`/usuarios/${id}`);
        if (!res.ok) throw new Error('Erro ao buscar dados do usuário.');
        const u = await res.json();
        
        // Mudar título do modal e texto do botão
        document.querySelector('#usuarioModal .modal-title').innerHTML = '<i class="bi bi-pencil-square me-2 text-accent"></i>Editar Usuário';
        document.getElementById('btnSaveUsuario').innerHTML = '<i class="bi bi-check-circle me-1"></i>Salvar Alterações';
        
        // Preencher dados básicos
        document.getElementById('usuarioNome').value = u.nome;
        document.getElementById('usuarioEmail').value = u.email;
        document.getElementById('usuarioRole').value = u.role;
        document.getElementById('usuarioRole').disabled = true; // Impedir alteração de perfil
        
        toggleRoleFields();
        
        if (u.role === 'Aluno') {
            if (u.dataNascimento) {
                document.getElementById('usuarioDataNascimento').value = u.dataNascimento.split('T')[0];
            } else {
                document.getElementById('usuarioDataNascimento').value = '';
            }
            document.getElementById('usuarioTelefone').value = u.telefone || '';
            document.getElementById('usuarioNomeResponsavel').value = u.nomeResponsavel || '';
            document.getElementById('usuarioTelefoneResponsavel').value = u.telefoneResponsavel || '';
            document.getElementById('usuarioDocSaude').checked = u.documentacaoSaudeEntregue;
            document.getElementById('usuarioProblemasSaude').value = u.problemasSaude || '';
            document.getElementById('usuarioNivelPedagogico').value = u.nivelPedagogico || 'Iniciante';
            checkMinorStatus();
        } else if (u.role === 'Professor') {
            document.getElementById('usuarioCref').value = u.cref || '';
            document.getElementById('usuarioCrefAtivo').checked = u.crefAtivo;
            document.getElementById('usuarioAptoBebes').checked = u.aptoBebes;
            document.getElementById('usuarioAptoInfantil').checked = u.aptoInfantil;
            document.getElementById('usuarioAptoAdulto').checked = u.aptoAdulto;
            document.getElementById('usuarioAptoAltaPerformance').checked = u.aptoAltaPerformance;
            document.getElementById('usuarioAptoHidroginastica').checked = u.aptoHidroginastica;
            document.getElementById('usuarioAptoPcd').checked = u.aptoPcd;
            
            if (u.validadeSalvamentoAquatico) {
                document.getElementById('usuarioValidadeSalvamento').value = u.validadeSalvamentoAquatico.split('T')[0];
            } else {
                document.getElementById('usuarioValidadeSalvamento').value = '';
            }
            if (u.validadePrimeirosSocorros) {
                document.getElementById('usuarioValidadePrimeirosSocorros').value = u.validadePrimeirosSocorros.split('T')[0];
            } else {
                document.getElementById('usuarioValidadePrimeirosSocorros').value = '';
            }
        }
        
        if (window.bootstrap) {
            const modalEl = document.getElementById('usuarioModal');
            const modal = window.bootstrap.Modal.getOrCreateInstance(modalEl);
            modal._relatedTarget = null; // Limpa para não confundir com clique do botão "Novo Usuário"
            modal.show();
        }
    } catch (err) { 
        showToast(err.message, 'danger'); 
    }
}

export async function handleCreateUsuario(e) {
    e.preventDefault();
    const btn = e.target.querySelector('button[type="submit"]');
    if (btn) btn.disabled = true;
    try {
        const role = document.getElementById('usuarioRole').value;
        const payload = {
            nome: document.getElementById('usuarioNome').value,
            email: document.getElementById('usuarioEmail').value
        };

        if (role === 'Aluno') {
            payload.dataNascimento = document.getElementById('usuarioDataNascimento').value || null;
            payload.telefone = document.getElementById('usuarioTelefone').value || null;
            payload.nomeResponsavel = document.getElementById('usuarioNomeResponsavel').value || null;
            payload.telefoneResponsavel = document.getElementById('usuarioTelefoneResponsavel').value || null;
            payload.documentacaoSaudeEntregue = document.getElementById('usuarioDocSaude').checked;
            payload.problemasSaude = document.getElementById('usuarioProblemasSaude').value || null;
            payload.nivelPedagogico = document.getElementById('usuarioNivelPedagogico').value || 'Iniciante';
        } else if (role === 'Professor') {
            payload.cref = document.getElementById('usuarioCref').value || null;
            payload.crefAtivo = document.getElementById('usuarioCrefAtivo').checked;
            payload.aptoBebes = document.getElementById('usuarioAptoBebes').checked;
            payload.aptoInfantil = document.getElementById('usuarioAptoInfantil').checked;
            payload.aptoAdulto = document.getElementById('usuarioAptoAdulto').checked;
            payload.aptoAltaPerformance = document.getElementById('usuarioAptoAltaPerformance').checked;
            payload.aptoHidroginastica = document.getElementById('usuarioAptoHidroginastica').checked;
            payload.aptoPcd = document.getElementById('usuarioAptoPcd').checked;
            payload.validadeSalvamentoAquatico = document.getElementById('usuarioValidadeSalvamento').value || null;
            payload.validadePrimeirosSocorros = document.getElementById('usuarioValidadePrimeirosSocorros').value || null;
        }

        let res;
        if (editingUsuarioId) {
            // Edição
            res = await apiFetch(`/usuarios/${editingUsuarioId}`, {
                method: 'PUT',
                body: JSON.stringify(payload)
            });
        } else {
            // Criação
            payload.role = role;
            res = await apiFetch('/usuarios', {
                method: 'POST',
                body: JSON.stringify(payload)
            });
        }

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Erro ao salvar usuário.');
        }

        if (window.bootstrap) {
            window.bootstrap.Modal.getInstance(document.getElementById('usuarioModal')).hide();
        }
        document.getElementById('usuarioForm').reset();
        
        if (editingUsuarioId) {
            showToast('Cadastro atualizado com sucesso! 👍', 'success');
        } else {
            showToast('Usuário cadastrado! Senha gerada automaticamente e enviada por e-mail. 📧', 'success');
        }
        
        editingUsuarioId = null;
        loadUsuarios();
    } catch (err) { 
        showToast(err.message, 'danger'); 
    } finally {
        if (btn) btn.disabled = false;
    }
}

export function toggleRoleFields() {
    const role = document.getElementById('usuarioRole').value;
    const alunoFields = document.getElementById('alunoFields');
    const professorFields = document.getElementById('professorFields');
    
    const dataNascimento = document.getElementById('usuarioDataNascimento');
    const telefone = document.getElementById('usuarioTelefone');
    const cref = document.getElementById('usuarioCref');
    
    if (role === 'Aluno') {
        alunoFields.style.display = '';
        professorFields.style.display = 'none';
        
        dataNascimento.required = true;
        telefone.required = true;
        cref.required = false;
        
        checkMinorStatus();
    } else if (role === 'Professor') {
        alunoFields.style.display = 'none';
        professorFields.style.display = '';
        
        dataNascimento.required = false;
        telefone.required = false;
        cref.required = false;
    } else {
        alunoFields.style.display = 'none';
        professorFields.style.display = 'none';
        
        dataNascimento.required = false;
        telefone.required = false;
        cref.required = false;
    }
}

export function checkMinorStatus() {
    const dobValue = document.getElementById('usuarioDataNascimento').value;
    const grupoResponsavel = document.getElementById('grupoResponsavel');
    const nomeResp = document.getElementById('usuarioNomeResponsavel');
    const telResp = document.getElementById('usuarioTelefoneResponsavel');
    
    if (!dobValue) {
        grupoResponsavel.style.display = 'none';
        nomeResp.required = false;
        telResp.required = false;
        return;
    }
    
    const dob = new Date(dobValue);
    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const m = today.getMonth() - dob.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < dob.getDate())) {
        age--;
    }
    
    if (age < 18) {
        grupoResponsavel.style.display = '';
        nomeResp.required = true;
        telResp.required = true;
    } else {
        grupoResponsavel.style.display = 'none';
        nomeResp.required = false;
        telResp.required = false;
        nomeResp.value = '';
        telResp.value = '';
    }
}

export async function deleteUsuario(id, nome) {
    if (!confirm(`Excluir o usuário "${nome}"?`)) return;
    try {
        const res = await apiFetch(`/usuarios/${id}`, { method: 'DELETE' });
        if (!res.ok && res.status !== 204) throw new Error('Erro ao excluir.');
        showToast('Usuário excluído. 🗑️', 'warning');
        loadUsuarios();
    } catch (err) { 
        showToast(err.message, 'danger'); 
    }
}

export function openNivelModal(id, currentNivel) {
    document.getElementById('nivelUsuarioId').value = id;
    document.getElementById('alunoNivelSelect').value = currentNivel || 'Iniciante';
    const modalEl = document.getElementById('nivelModal');
    if (window.bootstrap) {
        window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }
}

export function openNivelModalFromEvent(id, currentNivel) {
    if (window.bootstrap) {
        window.bootstrap.Modal.getInstance(document.getElementById('eventoModal'))?.hide();
    }
    openNivelModal(id, currentNivel);
}

export async function handleNivelSubmit(e) {
    e.preventDefault();
    const id = document.getElementById('nivelUsuarioId').value;
    const nivel = document.getElementById('alunoNivelSelect').value;

    try {
        const res = await apiFetch(`/usuarios/${id}/nivel`, {
            method: 'PUT',
            body: JSON.stringify({ nivelPedagogico: nivel })
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Erro ao atualizar nível.');
        }

        if (window.bootstrap) {
            window.bootstrap.Modal.getOrCreateInstance(document.getElementById('nivelModal')).hide();
        }
        showToast('Nível pedagógico atualizado! 🏆', 'success');
        
        loadUsuarios();
        if (calendarInstance) {
            calendarInstance.refetchEvents();
        }
    } catch (err) {
        showToast(err.message, 'danger');
    }
}

// Iniciar eventos associados a fechar e abrir modal de usuário
document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('usuarioModal');
    if (modal) {
        modal.addEventListener('hidden.bs.modal', () => {
            const form = document.getElementById('usuarioForm');
            if (form) form.reset();
            document.getElementById('usuarioRole').disabled = false;
            editingUsuarioId = null;
            document.querySelector('#usuarioModal .modal-title').innerHTML = '<i class="bi bi-person-plus me-2 text-accent"></i>Novo Usuário';
            document.getElementById('btnSaveUsuario').innerHTML = '<i class="bi bi-person-plus me-1"></i>Cadastrar';
        });

        modal.addEventListener('show.bs.modal', (e) => {
            const triggerEl = e.relatedTarget;
            if (triggerEl && triggerEl.id === 'btnNovoUsuario') {
                editingUsuarioId = null;
                const form = document.getElementById('usuarioForm');
                if (form) form.reset();
                document.getElementById('usuarioRole').disabled = false;
                document.querySelector('#usuarioModal .modal-title').innerHTML = '<i class="bi bi-person-plus me-2 text-accent"></i>Novo Usuário';
                document.getElementById('btnSaveUsuario').innerHTML = '<i class="bi bi-person-plus me-1"></i>Cadastrar';
                toggleRoleFields();
            }
        });
    }
});
