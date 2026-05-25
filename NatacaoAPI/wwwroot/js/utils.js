// ═══════════════════════════════════════════════════════════════
// UTILS MODULE
// ═══════════════════════════════════════════════════════════════

export function togglePasswordVisibility(inputId, iconEl) {
    const input = document.getElementById(inputId);
    if (!input) return;
    if (input.type === 'password') {
        input.type = 'text';
        iconEl.classList.remove('bi-eye');
        iconEl.classList.add('bi-eye-slash');
    } else {
        input.type = 'password';
        iconEl.classList.remove('bi-eye-slash');
        iconEl.classList.add('bi-eye');
    }
}

export function showAuthAlert(message, type) {
    const alert = document.getElementById('authAlert');
    if (!alert) return;
    alert.style.display = '';
    const bgClass = type === 'success'
        ? 'background: rgba(16,185,129,0.1); border: 1px solid rgba(16,185,129,0.3); color: #34d399;'
        : 'background: rgba(239,68,68,0.1); border: 1px solid rgba(239,68,68,0.3); color: #fca5a5;';
    alert.innerHTML = `<div class="alert mb-0" style="${bgClass} border-radius: 10px; font-size: 0.85rem; padding: 12px 16px;">
        ${escapeHtml(message)}</div>`;
}

export function hideAuthAlert() {
    const alert = document.getElementById('authAlert');
    if (alert) alert.style.display = 'none';
}

export function showToast(message, type = 'success') {
    const toast = document.getElementById('appToast');
    if (!toast) return;
    toast.className = `toast align-items-center border-0 bg-${type}-toast text-white`;
    document.getElementById('toastMessage').textContent = message;
    
    // Utilizando o objeto bootstrap disponível globalmente
    if (window.bootstrap) {
        window.bootstrap.Toast.getOrCreateInstance(toast, { delay: 4000 }).show();
    }
}

export function setBtnLoading(btn, loading) {
    if (!btn) return;
    const text = btn.querySelector('.btn-text');
    const loader = btn.querySelector('.btn-loader');
    if (text) text.style.display = loading ? 'none' : '';
    if (loader) loader.style.display = loading ? '' : 'none';
    btn.disabled = loading;
}

export function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

export function getTableSkeletonHtml(cols, rows = 3) {
    let rowsHtml = '';
    for (let r = 0; r < rows; r++) {
        let colsHtml = '';
        for (let c = 0; c < cols; c++) {
            const widths = ['w-50', 'w-75', 'w-100', 'w-25'];
            const w = widths[(r + c) % widths.length];
            colsHtml += `<td><div class="skeleton-bar ${w}"></div></td>`;
        }
        rowsHtml += `<tr class="skeleton-row">${colsHtml}</tr>`;
    }
    return rowsHtml;
}

export function calcularIdade(dobString) {
    if (!dobString) return 0;
    const dob = new Date(dobString);
    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const m = today.getMonth() - dob.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < dob.getDate())) {
        age--;
    }
    return age;
}

export function getProfessorSpecialties(profName, modalidade) {
    if (!profName) return '';
    const name = profName.toLowerCase();
    
    // Mapeamento customizado para professores conhecidos no sistema
    if (name.includes('segurança')) {
        return 'Segurança Aquática, Adulto, Hidroginástica';
    }
    if (name.includes('nivel') || name.includes('teste')) {
        return 'Adulto, Alta Performance, Hidroginástica';
    }
    
    // Mapeamento dinâmico baseado na modalidade
    const specs = [modalidade];
    if (modalidade === 'Bebês' || modalidade === 'Bebê') {
        specs.push('Infantil');
        specs.push('PCD');
    } else if (modalidade === 'Adulto') {
        specs.push('Hidroginástica');
        specs.push('Alta Performance');
    } else if (modalidade === 'Hidroginástica' || modalidade === 'Hidroginastica') {
        specs.push('Adulto');
    } else if (modalidade === 'Infantil') {
        specs.push('Bebês');
    } else if (modalidade === 'Alta Performance') {
        specs.push('PCD');
    }
    return specs.join(', ');
}
