/// <reference types="cypress" />

describe('04 - Reservas (Aluno)', () => {
    const ts = Date.now();
    const profEmail = `prof.reservas.${ts}@test.com`;
    const alunoEmail = `aluno.reservas.${ts}@test.com`;
    let profUserId, alunoUserId, turmaId, profToken;

    before(() => {
        // Criar professor
        cy.apiCreateUser({ nome: 'Prof. Reservas', email: profEmail, senha: 'Prof@123!', role: 'Professor' }).then(res => {
            profUserId = res.body.id;
            // Login como professor para criar turma
            cy.request('POST', '/api/auth/login', { email: profEmail, senha: 'Prof@123!' }).then(loginRes => {
                profToken = loginRes.body.token;
                const tomorrow = new Date();
                tomorrow.setDate(tomorrow.getDate() + 1);
                cy.apiCreateTurma(profToken, {
                    nome: 'Turma Reservas Test',
                    descricao: 'Para testes de reserva',
                    modalidade: 'Adulto',
                    dataHoraInicio: tomorrow.toISOString(),
                    dataHoraFim: new Date(tomorrow.getTime() + 3600000).toISOString(),
                    capacidadeMaxima: 5
                }).then(turmaRes => {
                    turmaId = turmaRes.body.id;
                });
            });
        });
        // Criar aluno
        cy.apiCreateUser({ nome: 'Aluno Reservas', email: alunoEmail, senha: 'Aluno@123!', role: 'Aluno' }).then(res => {
            alunoUserId = res.body.id;
        });
    });

    after(() => {
        if (profUserId) cy.apiDeleteUser(profUserId);
        if (alunoUserId) cy.apiDeleteUser(alunoUserId);
    });

    beforeEach(() => {
        cy.clearLocalStorage();
        cy.apiLogin(alunoEmail, 'Aluno@123!');
        cy.visit('/dashboard.html');
    });

    it('Deve criar uma reserva via API', () => {
        cy.apiLogin(alunoEmail, 'Aluno@123!').then(aluno => {
            cy.request({
                method: 'POST',
                url: '/api/reservas',
                headers: { Authorization: `Bearer ${aluno.token}` },
                body: { turmaId }
            }).then(res => {
                expect(res.status).to.eq(201);
                expect(res.body).to.have.property('id');
            });
        });
    });

    it('Deve exibir reservas na seção Reservas', () => {
        cy.get('[data-section="reservas"]').click();
        cy.get('#reservasBody').should('be.visible');
        cy.get('#reservasBody tr').should('have.length.greaterThan', 0);
    });

    it('Deve cancelar uma reserva', () => {
        cy.apiLogin(alunoEmail, 'Aluno@123!').then(aluno => {
            // Obter reservas do aluno
            cy.request({
                url: '/api/reservas',
                headers: { Authorization: `Bearer ${aluno.token}` }
            }).then(res => {
                const ativa = res.body.find(r => r.status === 'Ativa');
                if (ativa) {
                    cy.request({
                        method: 'DELETE',
                        url: `/api/reservas/${ativa.id}`,
                        headers: { Authorization: `Bearer ${aluno.token}` }
                    }).its('status').should('be.oneOf', [200, 204]);
                }
            });
        });
    });
});
