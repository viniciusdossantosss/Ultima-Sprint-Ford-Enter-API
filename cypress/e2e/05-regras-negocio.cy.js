/// <reference types="cypress" />

describe('05 - Regras de Negócio', () => {
    const ts = Date.now();
    const profEmail = `prof.rules.${ts}@test.com`;
    const aluno1Email = `aluno1.rules.${ts}@test.com`;
    const aluno2Email = `aluno2.rules.${ts}@test.com`;
    let profUserId, aluno1UserId, aluno2UserId, profToken;

    before(() => {
        cy.apiCreateUser({ nome: 'Prof Rules', email: profEmail, senha: 'Prof@123!', role: 'Professor' }).then(r => {
            profUserId = r.body.id;
        });
        cy.apiCreateUser({ nome: 'Aluno1 Rules', email: aluno1Email, senha: 'Aluno@123!', role: 'Aluno' }).then(r => {
            aluno1UserId = r.body.id;
        });
        cy.apiCreateUser({ nome: 'Aluno2 Rules', email: aluno2Email, senha: 'Aluno@123!', role: 'Aluno' }).then(r => {
            aluno2UserId = r.body.id;
        });
    });

    after(() => {
        [profUserId, aluno1UserId, aluno2UserId].forEach(id => { if (id) cy.apiDeleteUser(id); });
    });

    it('RF001: Deve rejeitar reserva quando turma está lotada', () => {
        // Login como professor e criar turma com capacidade 1
        cy.request('POST', '/api/auth/login', { email: profEmail, senha: 'Prof@123!' }).then(res => {
            profToken = res.body.token;
            const tomorrow = new Date();
            tomorrow.setDate(tomorrow.getDate() + 2);

            cy.apiCreateTurma(profToken, {
                nome: 'Turma Lotada RF001',
                descricao: 'Capacidade = 1',
                modalidade: 'Adulto',
                dataHoraInicio: tomorrow.toISOString(),
                dataHoraFim: new Date(tomorrow.getTime() + 3600000).toISOString(),
                capacidadeMaxima: 1
            }).then(turmaRes => {
                const turmaId = turmaRes.body.id;

                // Aluno 1 reserva com sucesso
                cy.request('POST', '/api/auth/login', { email: aluno1Email, senha: 'Aluno@123!' }).then(a1 => {
                    cy.request({
                        method: 'POST', url: '/api/reservas',
                        headers: { Authorization: `Bearer ${a1.body.token}` },
                        body: { turmaId }
                    }).its('status').should('eq', 201);

                    // Aluno 2 tenta reservar → deve falhar (400)
                    cy.request('POST', '/api/auth/login', { email: aluno2Email, senha: 'Aluno@123!' }).then(a2 => {
                        cy.request({
                            method: 'POST', url: '/api/reservas',
                            headers: { Authorization: `Bearer ${a2.body.token}` },
                            body: { turmaId },
                            failOnStatusCode: false
                        }).then(r => {
                            expect(r.status).to.eq(400);
                            expect(r.body.message).to.include('RF001');
                        });
                    });
                });
            });
        });
    });

    it('RF002: Deve rejeitar reserva com conflito de horário', () => {
        cy.request('POST', '/api/auth/login', { email: profEmail, senha: 'Prof@123!' }).then(res => {
            profToken = res.body.token;
            const day = new Date();
            day.setDate(day.getDate() + 3);
            const inicio = new Date(day); inicio.setHours(10, 0, 0);
            const fim = new Date(day); fim.setHours(11, 0, 0);

            // Criar turma 1
            cy.apiCreateTurma(profToken, {
                nome: 'Turma Conflito A', descricao: '', modalidade: 'Adulto',
                dataHoraInicio: inicio.toISOString(), dataHoraFim: fim.toISOString(),
                capacidadeMaxima: 10
            }).then(t1 => {
                // Criar turma 2 no mesmo horário
                cy.apiCreateTurma(profToken, {
                    nome: 'Turma Conflito B', descricao: '', modalidade: 'Infantil',
                    dataHoraInicio: inicio.toISOString(), dataHoraFim: fim.toISOString(),
                    capacidadeMaxima: 10
                }).then(t2 => {
                    // Aluno reserva turma 1
                    cy.request('POST', '/api/auth/login', { email: aluno1Email, senha: 'Aluno@123!' }).then(a1 => {
                        cy.request({
                            method: 'POST', url: '/api/reservas',
                            headers: { Authorization: `Bearer ${a1.body.token}` },
                            body: { turmaId: t1.body.id }
                        }).its('status').should('eq', 201);

                        // Aluno tenta reservar turma 2 → conflito (400)
                        cy.request({
                            method: 'POST', url: '/api/reservas',
                            headers: { Authorization: `Bearer ${a1.body.token}` },
                            body: { turmaId: t2.body.id },
                            failOnStatusCode: false
                        }).then(r => {
                            expect(r.status).to.eq(400);
                            expect(r.body.message).to.include('RF002');
                        });
                    });
                });
            });
        });
    });

    it('Deve permitir reserva após cancelamento liberar vaga', () => {
        cy.request('POST', '/api/auth/login', { email: profEmail, senha: 'Prof@123!' }).then(res => {
            const day = new Date();
            day.setDate(day.getDate() + 4);

            cy.apiCreateTurma(res.body.token, {
                nome: 'Turma Vaga Liberada', descricao: '', modalidade: 'Livre',
                dataHoraInicio: day.toISOString(),
                dataHoraFim: new Date(day.getTime() + 3600000).toISOString(),
                capacidadeMaxima: 1
            }).then(turmaRes => {
                const turmaId = turmaRes.body.id;

                // Aluno 1 reserva
                cy.request('POST', '/api/auth/login', { email: aluno1Email, senha: 'Aluno@123!' }).then(a1 => {
                    cy.request({
                        method: 'POST', url: '/api/reservas',
                        headers: { Authorization: `Bearer ${a1.body.token}` },
                        body: { turmaId }
                    }).then(reservaRes => {
                        // Aluno 1 cancela
                        cy.request({
                            method: 'DELETE', url: `/api/reservas/${reservaRes.body.id}`,
                            headers: { Authorization: `Bearer ${a1.body.token}` }
                        });

                        // Aluno 2 agora pode reservar
                        cy.request('POST', '/api/auth/login', { email: aluno2Email, senha: 'Aluno@123!' }).then(a2 => {
                            cy.request({
                                method: 'POST', url: '/api/reservas',
                                headers: { Authorization: `Bearer ${a2.body.token}` },
                                body: { turmaId }
                            }).its('status').should('eq', 201);
                        });
                    });
                });
            });
        });
    });
});
