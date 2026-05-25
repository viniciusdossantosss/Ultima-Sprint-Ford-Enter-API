// ═══════════════════════════════════════════════════════════════
// AUTH MODULE
// ═══════════════════════════════════════════════════════════════

import { API_BASE, setCurrentUser } from './api.js';
import { showAuthAlert, hideAuthAlert, setBtnLoading } from './utils.js';

export async function handleLogin(e) {
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

        const currentUser = await res.json();
        setCurrentUser(currentUser);
        window.location.href = 'dashboard.html';
    } catch (err) {
        showAuthAlert(err.message, 'danger');
    } finally {
        setBtnLoading(btn, false);
    }
}

export function showForgotPassword(e) {
    e?.preventDefault();
    document.getElementById('loginView').style.display = 'none';
    document.getElementById('forgotView').style.display = '';
    document.getElementById('resetView').style.display = 'none';
    hideAuthAlert();
}

export function showLogin(e) {
    e?.preventDefault();
    document.getElementById('loginView').style.display = '';
    document.getElementById('forgotView').style.display = 'none';
    document.getElementById('resetView').style.display = 'none';
    hideAuthAlert();
}

export function showResetPassword(token) {
    document.getElementById('loginView').style.display = 'none';
    document.getElementById('forgotView').style.display = 'none';
    document.getElementById('resetView').style.display = '';
    document.getElementById('resetView').dataset.token = token;
}

export async function handleForgotPassword(e) {
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

export async function handleResetPassword(e) {
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

export function logout() {
    setCurrentUser(null);
    window.location.href = '/';
}
