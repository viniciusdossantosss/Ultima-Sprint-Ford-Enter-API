/// <reference types="cypress" />

describe('03 - Turmas (Professor)', () => {
    const profEmail = `prof.turmas.${Date.now()}@test.com`;
    let profToken = null;
    let profUserId = null;

    before(() => {
        cy.apiCreateUser({
            nome: 'Prof. Turmas Test',
            email: profEmail,
            senha: 'Prof@123!',
            role: 'Professor'
        }).then(res => {
            profUserId = res.body.id;
        });
    });

    after(() => {
        if (profUserId) cy.apiDeleteUser(profUserId);
    });

    beforeEach(() => {
        cy.clearLocalStorage();
        cy.apiLogin(profEmail, 'Prof@123!').then(u => { profToken = u.token; });
        cy.visit('/dashboard.html');
    });

    it('Deve exibir botão Nova Turma para Professor', () => {
        cy.get('#btnNovaTurma').should('be.visible');
    });

    it('Deve criar uma nova turma via modal', () => {
        cy.get('#btnNovaTurma').click();
        cy.get('#turmaModal').should('be.visible');

        cy.get('#turmaNome').type('Turma Cypress A');
        cy.get('#turmaDescricao').type('Turma criada pelo Cypress');
        cy.get('#turmaModalidade').select('Adulto');

        // Data futura
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);
        const dateStr = tomorrow.toISOString().substring(0, 10);
        cy.get('#turmaInicio').type(`${dateStr}T08:00`);
        cy.get('#turmaFim').type(`${dateStr}T09:00`);
        cy.get('#turmaCapacidade').clear().type('5');

        cy.intercept('POST', '/api/turmas').as('createTurma');
        cy.get('#btnSaveTurma').click();
        cy.wait('@createTurma').its('response.statusCode').should('eq', 201);
        cy.get('#appToast').should('be.visible');
    });

    it('Deve exibir turma criada no calendário', () => {
        // Criar turma via API para garantir
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);
        cy.apiCreateTurma(profToken, {
            nome: 'Turma Calendar Test',
            descricao: 'Teste calendário',
            modalidade: 'Infantil',
            dataHoraInicio: tomorrow.toISOString(),
            dataHoraFim: new Date(tomorrow.getTime() + 3600000).toISOString(),
            capacidadeMaxima: 10
        });

        cy.visit('/dashboard.html');
        cy.get('#calendar').should('be.visible');
        // O FullCalendar deve renderizar eventos
        cy.get('.fc-event', { timeout: 10000 }).should('have.length.greaterThan', 0);
    });

    it('NÃO deve exibir Nova Turma para Aluno', () => {
        const alunoEmail = `aluno.noturma.${Date.now()}@test.com`;
        cy.apiCreateUser({ nome: 'Aluno No Turma', email: alunoEmail, senha: 'Aluno@123!', role: 'Aluno' }).then(res => {
            cy.clearLocalStorage();
            cy.apiLogin(alunoEmail, 'Aluno@123!');
            cy.visit('/dashboard.html');
            cy.get('#btnNovaTurma').should('not.be.visible');
            cy.apiDeleteUser(res.body.id);
        });
    });
});
