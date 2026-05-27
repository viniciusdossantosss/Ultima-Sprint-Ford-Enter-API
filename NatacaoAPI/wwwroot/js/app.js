// ═══════════════════════════════════════════════════════════════
// MAIN APPLICATION COORDINATOR
// ═══════════════════════════════════════════════════════════════

import { getCurrentUser } from './api.js';
import { togglePasswordVisibility, calcularIdade } from './utils.js';
import { handleLogin, showForgotPassword, showLogin, showResetPassword, handleForgotPassword, handleResetPassword, logout } from './auth.js';
import { loadPerfil, habilitarEdicaoPerfil, handlePerfilSubmit, loadAlertasCertificacao } from './profile.js';
import { initCalendar, handleTurmaSubmit, editarTurma, deletarTurma, populateProfessorsDropdown } from './calendar.js';
import { loadReservas, criarReserva, cancelarReserva } from './reservas.js';
import { loadUsuarios, filtrarUsuarios, editUsuario, handleCreateUsuario, toggleRoleFields, checkMinorStatus, openNivelModal, openNivelModalFromEvent, handleNivelSubmit, deleteUsuario } from './users.js';

// ─── Mock do Objeto Bootstrap para Modais e Toasts ───────────
const bootstrap = {
    Modal: class {
        constructor(element) {
            const el = typeof element === 'string' ? document.getElementById(element) : element;
            if (el && el.__modalInstance) {
                return el.__modalInstance;
            }
            this.element = el;
            this._relatedTarget = null;
            if (this.element) {
                this.element.__modalInstance = this;
                
                // Clique no backdrop para fechar
                this.element.addEventListener('click', (e) => {
                    if (e.target === this.element) {
                        this.hide();
                    }
                });
            }
        }
        show() {
            if (this.element) {
                const showEvent = new CustomEvent('show.bs.modal', { 
                    bubbles: true, 
                    cancelable: true
                });
                Object.defineProperty(showEvent, 'relatedTarget', {
                    value: this._relatedTarget,
                    writable: false
                });
                this.element.dispatchEvent(showEvent);

                this.element.style.display = 'flex';
                this.element.offsetHeight; // Force reflow
                this.element.classList.add('show');
                document.body.style.overflow = 'hidden';
            }
        }
        hide() {
            if (this.element) {
                this.element.classList.remove('show');
                setTimeout(() => {
                    if (!this.element.classList.contains('show')) {
                        this.element.style.display = 'none';
                        if (!document.querySelector('.modal.show')) {
                            document.body.style.overflow = '';
                        }
                        const hiddenEvent = new CustomEvent('hidden.bs.modal', { bubbles: true });
                        this.element.dispatchEvent(hiddenEvent);
                    }
                }, 300);
            }
        }
        static getOrCreateInstance(el) {
            return bootstrap.Modal.getInstance(el) || new bootstrap.Modal(el);
        }
        static getInstance(el) {
            if (!el) return null;
            return el.__modalInstance || null;
        }
    },
    Toast: class {
        constructor(element, options = {}) {
            this.element = typeof element === 'string' ? document.getElementById(element) : element;
            this.delay = options.delay || 4000;
        }
        show() {
            if (this.element) {
                this.element.style.display = 'block';
                this.element.offsetHeight; // Force reflow
                this.element.classList.add('show');
                
                const closeBtn = this.element.querySelector('.btn-close');
                if (closeBtn) {
                    closeBtn.onclick = () => this.hide();
                }

                if (this._timeout) clearTimeout(this._timeout);
                this._timeout = setTimeout(() => this.hide(), this.delay);
            }
        }
        hide() {
            if (this.element) {
                this.element.classList.remove('show');
                setTimeout(() => {
                    if (!this.element.classList.contains('show')) {
                        this.element.style.display = 'none';
                    }
                }, 300);
            }
        }
        static getOrCreateInstance(el, options) {
            return new bootstrap.Toast(el, options);
        }
    }
};

window.bootstrap = bootstrap;

// Declarative trigger click listener para Modais
document.addEventListener('click', (e) => {
    const trigger = e.target.closest('[data-bs-toggle="modal"]');
    if (trigger) {
        e.preventDefault();
        const targetSelector = trigger.getAttribute('data-bs-target');
        const modalEl = document.querySelector(targetSelector);
        if (modalEl) {
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal._relatedTarget = trigger;
            modal.show();
        }
    }

    const dismiss = e.target.closest('[data-bs-dismiss="modal"]');
    if (dismiss) {
        e.preventDefault();
        const modalEl = dismiss.closest('.modal');
        if (modalEl) {
            const modal = bootstrap.Modal.getInstance(modalEl);
            if (modal) modal.hide();
        }
    }
});

// Auto-inicializar todos os modais da página
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.modal').forEach(modalEl => {
        bootstrap.Modal.getOrCreateInstance(modalEl);
    });
});

// ─── Session Page Detection & Boot ───────────────────────────
const isLoginPage = !document.getElementById('dashboardSection') && document.getElementById('authSection');
const isDashboardPage = document.getElementById('sectionTurmas') != null;

document.addEventListener('DOMContentLoaded', () => {
    const currentUser = getCurrentUser();

    if (isLoginPage) {
        const params = new URLSearchParams(window.location.search);
        const resetToken = params.get('resetToken');
        if (resetToken) {
            showResetPassword(resetToken);
            return;
        }
        if (currentUser) {
            window.location.href = 'dashboard.html';
        }
    } else if (isDashboardPage) {
        if (!currentUser) {
            window.location.href = '/';
            return;
        }
        initDashboard(currentUser);
    }
});

// ─── Dashboard Core Init ─────────────────────────────────────
function initDashboard(currentUser) {
    document.body.className = `role-${currentUser.role.toLowerCase()}`;
    document.getElementById('navUserName').textContent = currentUser.nome;
    const roleMap = { Admin: '👑 Admin', Professor: '👨‍🏫 Professor', Aluno: '🏊 Aluno' };
    document.getElementById('navUserRole').textContent = roleMap[currentUser.role] || currentUser.role;
    
    const statRoleEl = document.getElementById('statRole');
    if (statRoleEl) {
        const statRoleLabel = statRoleEl.previousElementSibling;
        if (currentUser.role === 'Aluno') {
            if (statRoleLabel) statRoleLabel.textContent = 'Nível Pedagógico';
            statRoleEl.textContent = currentUser.nivelPedagogico || 'Iniciante';
        } else {
            if (statRoleLabel) statRoleLabel.textContent = 'Seu Perfil';
            statRoleEl.textContent = roleMap[currentUser.role] || currentUser.role;
        }
    }

    const isAdmin = currentUser.role === 'Admin';
    const isProfessor = currentUser.role === 'Professor';

    document.querySelectorAll('.admin-only').forEach(el => {
        el.style.display = isAdmin ? '' : 'none';
    });
    document.querySelectorAll('.professor-only').forEach(el => {
        el.style.display = (isProfessor || isAdmin) ? '' : 'none';
    });
    document.querySelectorAll('.aluno-only').forEach(el => {
        el.style.display = (!isProfessor && !isAdmin) ? '' : 'none';
    });

    const grupoTurmaProfessor = document.getElementById('grupoTurmaProfessor');
    if (grupoTurmaProfessor) {
        grupoTurmaProfessor.style.display = isAdmin ? '' : 'none';
    }

    if (isProfessor || isAdmin) {
        const titleRes = document.getElementById('reservasTitle');
        const lblRes = document.getElementById('statReservasLabel');
        if (titleRes) titleRes.textContent = 'Todas as Reservas';
        if (lblRes) lblRes.textContent = 'Total de Reservas';
    }

    initCalendar();
    loadReservas();
    initNotifications(currentUser);
    
    const tabUsuarios = document.querySelector('.nav-tab[data-section="usuarios"]');
    const headerUsuarios = document.querySelector('#sectionUsuarios h2');
    
    if (isAdmin) {
        if (tabUsuarios) {
            tabUsuarios.style.display = '';
            tabUsuarios.querySelector('span').textContent = 'Usuários';
        }
        if (headerUsuarios) {
            headerUsuarios.innerHTML = '<i class="bi bi-people me-2"></i>Gestão de Usuários';
        }
        const btnNovo = document.getElementById('btnNovoUsuario');
        if (btnNovo) btnNovo.style.display = '';

        loadUsuarios();
        loadAlertasCertificacao();
        populateProfessorsDropdown();
    } else if (isProfessor) {
        if (tabUsuarios) {
            tabUsuarios.style.display = '';
            tabUsuarios.querySelector('span').textContent = 'Alunos';
        }
        if (headerUsuarios) {
            headerUsuarios.innerHTML = '<i class="bi bi-people me-2"></i>Gestão de Alunos';
        }
        const btnNovo = document.getElementById('btnNovoUsuario');
        if (btnNovo) btnNovo.style.display = 'none';

        loadUsuarios();
    } else {
        if (tabUsuarios) {
            tabUsuarios.style.display = 'none';
        }
    }

    document.getElementById('perfilDataNascimento')?.addEventListener('input', (e) => {
        const dobValue = e.target.value;
        const perfilGrupoResponsavel = document.getElementById('perfilGrupoResponsavel');
        const nomeResp = document.getElementById('perfilNomeResponsavel');
        const telResp = document.getElementById('perfilTelefoneResponsavel');
        
        if (!dobValue) {
            if (perfilGrupoResponsavel) perfilGrupoResponsavel.style.display = 'none';
            if (nomeResp) nomeResp.required = false;
            if (telResp) telResp.required = false;
            return;
        }
        const age = calcularIdade(dobValue);
        if (age < 18) {
            if (perfilGrupoResponsavel) perfilGrupoResponsavel.style.display = '';
            if (nomeResp) nomeResp.required = true;
            if (telResp) telResp.required = true;
        } else {
            if (perfilGrupoResponsavel) perfilGrupoResponsavel.style.display = 'none';
            if (nomeResp) {
                nomeResp.required = false;
                nomeResp.value = '';
            }
            if (telResp) {
                telResp.required = false;
                telResp.value = '';
            }
        }
    });
}

function switchSection(name) {
    document.querySelectorAll('.content-section').forEach(s => s.classList.remove('active'));
    document.querySelectorAll('.nav-tab').forEach(t => t.classList.remove('active'));

    const section = document.getElementById('section' + name.charAt(0).toUpperCase() + name.slice(1));
    if (section) section.classList.add('active');

    const tab = document.querySelector(`.nav-tab[data-section="${name}"]`);
    if (tab) tab.classList.add('active');

    if (name === 'perfil') {
        loadPerfil();
    }
}

// ─── Notificações Mockadas ───────────────────────────────────
let mockNotifications = [];

function initNotifications(currentUser) {
    const role = currentUser.role;
    if (role === 'Admin') {
        mockNotifications = [
            { id: 1, text: "O CREF do Professor Carlos Eduardo vence em 5 dias!", time: "Há 2 horas", unread: true },
            { id: 2, text: "Novo aluno cadastrado: Vinicius dos Santos.", time: "Há 1 dia", unread: false },
            { id: 3, text: "Reserva cancelada pelo aluno João Pedro na Turma A.", time: "Há 2 dias", unread: false }
        ];
    } else if (role === 'Professor') {
        mockNotifications = [
            { id: 1, text: "Sua turma de Infantil A amanhã às 09:00 tem 2 novas reservas.", time: "Há 1 hora", unread: true },
            { id: 2, text: "A validade do seu certificado de Salvamento Aquático expira em 30 dias.", time: "Há 5 horas", unread: true },
            { id: 3, text: "Nível pedagógico do aluno Pedro foi alterado para Avançado.", time: "Há 1 dia", unread: false }
        ];
    } else {
        mockNotifications = [
            { id: 1, text: "Sua reserva na Turma Iniciante A foi confirmada! 🏊", time: "Há 30 minutos", unread: true },
            { id: 2, text: "Boas-vindas ao AquaSchedule! Complete seu perfil para agendar aulas.", time: "Há 3 dias", unread: false }
        ];
    }
    renderNotifications();
}

function renderNotifications() {
    const notifBody = document.getElementById('notifBody');
    const notifBadge = document.getElementById('notifBadge');
    if (!notifBody) return;

    const unreadCount = mockNotifications.filter(n => n.unread).length;
    if (notifBadge) {
        if (unreadCount > 0) {
            notifBadge.style.display = '';
        } else {
            notifBadge.style.display = 'none';
        }
    }

    if (mockNotifications.length === 0) {
        notifBody.innerHTML = `
            <div class="notif-empty">
                <i class="bi bi-bell-slash text-muted"></i>
                <span>Sem novas notificações.</span>
            </div>
        `;
        return;
    }

    const escapeHtml = (text) => {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    };

    notifBody.innerHTML = mockNotifications.map(n => `
        <div class="notif-item ${n.unread ? 'unread' : ''}" onclick="toggleReadNotification(${n.id}, event)">
            <span class="notif-item-text">${escapeHtml(n.text)}</span>
            <span class="notif-item-time">${escapeHtml(n.time)}</span>
        </div>
    `).join('');
}

function toggleNotificationsDropdown(e) {
    e.stopPropagation();
    const dropdown = document.getElementById('notifDropdown');
    if (!dropdown) return;
    dropdown.style.display = dropdown.style.display === 'none' ? 'block' : 'none';
}

function toggleReadNotification(id, e) {
    e.stopPropagation();
    const notif = mockNotifications.find(n => n.id === id);
    if (notif) {
        notif.unread = !notif.unread;
        renderNotifications();
    }
}

function markAllNotificationsAsRead(e) {
    e.stopPropagation();
    mockNotifications.forEach(n => n.unread = false);
    renderNotifications();
    
    // Import showToast dynamically to show alert
    import('./utils.js').then(utils => {
        utils.showToast("Todas as notificações marcadas como lidas. 👍", "success");
    });
}

document.addEventListener('click', (e) => {
    const dropdown = document.getElementById('notifDropdown');
    if (dropdown && dropdown.style.display !== 'none') {
        const wrapper = document.querySelector('.nav-notification-wrapper');
        if (wrapper && !wrapper.contains(e.target)) {
            dropdown.style.display = 'none';
        }
    }
});

// ─── Exportações para o Escopo Global (HTML Inline Handlers) ─
window.togglePasswordVisibility = togglePasswordVisibility;
window.handleLogin = handleLogin;
window.showForgotPassword = showForgotPassword;
window.showLogin = showLogin;
window.handleForgotPassword = handleForgotPassword;
window.handleResetPassword = handleResetPassword;
window.logout = logout;
window.switchSection = switchSection;
window.habilitarEdicaoPerfil = habilitarEdicaoPerfil;
window.handlePerfilSubmit = handlePerfilSubmit;
window.criarReserva = criarReserva;
window.cancelarReserva = cancelarReserva;
window.editarTurma = editarTurma;
window.deletarTurma = deletarTurma;
window.handleTurmaSubmit = handleTurmaSubmit;
window.filtrarUsuarios = filtrarUsuarios;
window.editUsuario = editUsuario;
window.deleteUsuario = deleteUsuario;
window.handleCreateUsuario = handleCreateUsuario;
window.toggleRoleFields = toggleRoleFields;
window.checkMinorStatus = checkMinorStatus;
window.openNivelModal = openNivelModal;
window.openNivelModalFromEvent = openNivelModalFromEvent;
window.handleNivelSubmit = handleNivelSubmit;
window.toggleNotificationsDropdown = toggleNotificationsDropdown;
window.toggleReadNotification = toggleReadNotification;
window.markAllNotificationsAsRead = markAllNotificationsAsRead;
