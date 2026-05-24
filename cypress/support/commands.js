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
    cy.visit('/');
    cy.get('#loginEmail').clear().type(email);
    cy.get('#loginSenha').clear().type(senha);
    cy.get('#btnLogin').click();
    cy.url().should('include', 'dashboard');
});

// Criar usuário via API (como admin)
Cypress.Commands.add('apiCreateUser', (userData) => {
    return cy.apiLogin('admin@natacao.com', 'Admin@123').then(admin => {
        return cy.request({
            method: 'POST',
            url: '/api/usuarios',
            headers: { Authorization: `Bearer ${admin.token}` },
            body: userData,
            failOnStatusCode: false
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
