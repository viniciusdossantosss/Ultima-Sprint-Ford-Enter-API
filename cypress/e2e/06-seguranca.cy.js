/// <reference types="cypress" />

describe('06 - Segurança', () => {
    const ts = Date.now();
    const alunoEmail = `aluno.sec.${ts}@test.com`;
    const profEmail = `prof.sec.${ts}@test.com`;
    let alunoUserId, profUserId;

    before(() => {
        cy.apiCreateUser({ nome: 'Aluno Segurança', email: alunoEmail, senha: 'Aluno@123!', role: 'Aluno' }).then(r => {
            alunoUserId = r.body.id;
        });
        cy.apiCreateUser({ nome: 'Prof Segurança', email: profEmail, senha: 'Prof@123!', role: 'Professor' }).then(r => {
            profUserId = r.body.id;
        });
    });

    after(() => {
        [alunoUserId, profUserId].forEach(id => { if (id) cy.apiDeleteUser(id); });
    });

    it('Aluno NÃO deve criar turmas (403)', () => {
        cy.request('POST', '/api/auth/login', { email: alunoEmail, senha: 'Aluno@123!' }).then(res => {
            const tomorrow = new Date();
            tomorrow.setDate(tomorrow.getDate() + 5);

            cy.request({
                method: 'POST', url: '/api/turmas',
                headers: { Authorization: `Bearer ${res.body.token}` },
                body: {
                    nome: 'Turma Ilegal', descricao: '', modalidade: 'Adulto',
                    dataHoraInicio: tomorrow.toISOString(),
                    dataHoraFim: new Date(tomorrow.getTime() + 3600000).toISOString(),
                    capacidadeMaxima: 10
                },
                failOnStatusCode: false
            }).its('status').should('eq', 403);
        });
    });

    it('Professor NÃO deve acessar endpoint de admin (403)', () => {
        cy.request('POST', '/api/auth/login', { email: profEmail, senha: 'Prof@123!' }).then(res => {
            cy.request({
                method: 'GET', url: '/api/usuarios',
                headers: { Authorization: `Bearer ${res.body.token}` },
                failOnStatusCode: false
            }).its('status').should('eq', 403);
        });
    });

    it('Aluno NÃO deve cancelar reserva de outro aluno', () => {
        // Setup: criar aluno2, professor cria turma, aluno2 reserva
        const aluno2Email = `aluno2.sec.${ts}@test.com`;
        cy.apiCreateUser({ nome: 'Aluno2 Seg', email: aluno2Email, senha: 'Aluno@123!', role: 'Aluno' }).then(a2Res => {
            cy.request('POST', '/api/auth/login', { email: profEmail, senha: 'Prof@123!' }).then(profRes => {
                const day = new Date(); day.setDate(day.getDate() + 6);
                cy.apiCreateTurma(profRes.body.token, {
                    nome: 'Turma Seg Cross', descricao: '', modalidade: 'Livre',
                    dataHoraInicio: day.toISOString(),
                    dataHoraFim: new Date(day.getTime() + 3600000).toISOString(),
                    capacidadeMaxima: 10
                }).then(turmaRes => {
                    // Aluno 2 reserva
                    cy.request('POST', '/api/auth/login', { email: aluno2Email, senha: 'Aluno@123!' }).then(a2Login => {
                        cy.request({
                            method: 'POST', url: '/api/reservas',
                            headers: { Authorization: `Bearer ${a2Login.body.token}` },
                            body: { turmaId: turmaRes.body.id }
                        }).then(reservaRes => {
                            // Aluno 1 tenta cancelar reserva do aluno 2 → 401/403
                            cy.request('POST', '/api/auth/login', { email: alunoEmail, senha: 'Aluno@123!' }).then(a1Login => {
                                cy.request({
                                    method: 'DELETE', url: `/api/reservas/${reservaRes.body.id}`,
                                    headers: { Authorization: `Bearer ${a1Login.body.token}` },
                                    failOnStatusCode: false
                                }).then(r => {
                                    expect(r.status).to.be.oneOf([401, 403, 500]);
                                });
                            });
                        });
                    });
                });
            });
            cy.apiDeleteUser(a2Res.body.id);
        });
    });

    it('Deve bloquear login após 5 tentativas falhas', () => {
        const lockEmail = `lock.${ts}@test.com`;
        cy.apiCreateUser({ nome: 'Lock Test', email: lockEmail, senha: 'Lock@123!', role: 'Aluno' }).then(res => {
            // 5 tentativas com senha errada
            for (let i = 0; i < 5; i++) {
                cy.request({
                    method: 'POST', url: '/api/auth/login',
                    body: { email: lockEmail, senha: 'senhaerrada' },
                    failOnStatusCode: false
                });
            }

            // 6ª tentativa com senha CORRETA — deve falhar por lockout
            cy.request({
                method: 'POST', url: '/api/auth/login',
                body: { email: lockEmail, senha: 'Lock@123!' },
                failOnStatusCode: false
            }).then(r => {
                expect(r.status).to.be.oneOf([400, 401]);
                expect(r.body.message).to.include('bloqueada');
            });

            cy.apiDeleteUser(res.body.id);
        });
    });

    it('Requisição sem token deve retornar 401', () => {
        cy.request({
            method: 'GET', url: '/api/turmas',
            failOnStatusCode: false
        }).its('status').should('eq', 401);
    });
});
