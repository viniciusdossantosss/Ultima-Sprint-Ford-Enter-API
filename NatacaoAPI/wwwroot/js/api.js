// ═══════════════════════════════════════════════════════════════
// API MODULE
// ═══════════════════════════════════════════════════════════════

import { showToast } from './utils.js';

export const API_BASE = '/api';

export function getCurrentUser() {
    const saved = localStorage.getItem('aquaUser');
    return saved ? JSON.parse(saved) : null;
}

export function setCurrentUser(user) {
    if (user) {
        localStorage.setItem('aquaUser', JSON.stringify(user));
    } else {
        localStorage.removeItem('aquaUser');
    }
}

export async function apiFetch(path, options = {}) {
    const headers = { 'Content-Type': 'application/json', ...options.headers };
    const currentUser = getCurrentUser();
    if (currentUser?.token) {
        headers['Authorization'] = `Bearer ${currentUser.token}`;
    }

    const res = await fetch(`${API_BASE}${path}`, { ...options, headers });

    if (res.status === 401) {
        showToast('Sessão expirada. Faça login novamente.', 'warning');
        setCurrentUser(null);
        window.location.href = '/';
        throw new Error('Sessão expirada.');
    }
    if (res.status === 403) {
        showToast('Sem permissão para esta ação.', 'danger');
        throw new Error('Permissão negada.');
    }
    return res;
}
