// ─── Custom Cypress Commands ──────────────────────────────────

// Login via API (rápido, para setup de testes)
Cypress.Commands.add('apiLogin', (email, senha) => {
    return cy.request({
        method: 'POST',
        url: '/api/auth/login',
        body: { email, senha }
    }).then(res => {
        const user = res.body;
        window.localStorage.setItem('aquaUser', JSON.stringify(user));
        return user;
    });
});

// Login via UI (testa a interface)
Cypress.Commands.add('uiLogin', (email, senha) => {
    cy.clearLocalStorage();
    cy.visit('/');
    cy.get('#loginEmail').clear().type(email);
    cy.get('#loginSenha').clear().type(senha);
    cy.get('#btnLogin').click();
    cy.url().should('include', 'dashboard');
});

// Criar usuário via API (como admin)
Cypress.Commands.add('apiCreateUser', (userData) => {
    const payload = { ...userData };
    if (payload.role === 'Aluno') {
        if (!payload.dataNascimento) payload.dataNascimento = '2000-01-01';
        if (!payload.telefone) payload.telefone = '(11) 99999-9999';
        if (!payload.senha) payload.senha = 'Aluno@123!';
    } else if (payload.role === 'Professor') {
        if (!payload.cref) payload.cref = '123456-G/SP';
        if (payload.crefAtivo === undefined) payload.crefAtivo = true;
        if (payload.aptoAdulto === undefined) payload.aptoAdulto = true;
        if (payload.aptoInfantil === undefined) payload.aptoInfantil = true;
        if (payload.aptoBebes === undefined) payload.aptoBebes = true;
        if (payload.aptoAltaPerformance === undefined) payload.aptoAltaPerformance = true;
        if (payload.aptoHidroginastica === undefined) payload.aptoHidroginastica = true;
        if (payload.aptoPcd === undefined) payload.aptoPcd = true;
        if (!payload.senha) payload.senha = 'Prof@123!';
    }
    return cy.apiLogin('admin@natacao.com', 'Admin@123').then(admin => {
        return cy.request({
            method: 'POST',
            url: '/api/usuarios',
            headers: { Authorization: `Bearer ${admin.token}` },
            body: payload,
            failOnStatusCode: false
        }).then(res => {
            if (res.status !== 201) {
                throw new Error(`apiCreateUser failed with status ${res.status}: ${JSON.stringify(res.body)}`);
            }
            return res;
        });
    });
});

// Criar turma via API (como professor)
Cypress.Commands.add('apiCreateTurma', (token, turmaData) => {
    return cy.request({
        method: 'POST',
        url: '/api/turmas',
        headers: { Authorization: `Bearer ${token}` },
        body: turmaData,
        failOnStatusCode: false
    });
});

// Cleanup: deletar usuário por ID (como admin)
Cypress.Commands.add('apiDeleteUser', (userId) => {
    return cy.apiLogin('admin@natacao.com', 'Admin@123').then(admin => {
        return cy.request({
            method: 'DELETE',
            url: `/api/usuarios/${userId}`,
            headers: { Authorization: `Bearer ${admin.token}` },
            failOnStatusCode: false
        });
    });
});
