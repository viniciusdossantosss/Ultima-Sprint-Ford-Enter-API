/// <reference types="cypress" />

describe('08 - Nível Pedagógico e Alunos Inscritos', () => {
    const ts = Date.now();
    const profEmail = `prof.nivel.${ts}@test.com`;
    const alunoEmail = `aluno.nivel.${ts}@test.com`;
    let profUserId, alunoUserId, turmaId, profToken;
    let createdUserIds = [];

    before(() => {
        // Criar Professor com CREF ativo e apto para modalidade Adulto
        cy.apiCreateUser({
            nome: 'Prof. Nivel Teste',
            email: profEmail,
            senha: 'Prof@123!',
            role: 'Professor',
            cref: '123456-G/SP',
            crefAtivo: true,
            aptoAdulto: true
        }).then(res => {
            profUserId = res.body.id;
            createdUserIds.push(profUserId);

            // Login como professor para criar uma turma amanhã
            cy.request('POST', '/api/auth/login', { email: profEmail, senha: 'Prof@123!' }).then(loginRes => {
                profToken = loginRes.body.token;
                const tomorrow = new Date();
                tomorrow.setDate(tomorrow.getDate() + 1);

                cy.apiCreateTurma(profToken, {
                    nome: `Turma Nivel E2E ${ts}`,
                    descricao: 'Turma de teste para verificar nível pedagógico',
                    modalidade: 'Adulto',
                    dataHoraInicio: tomorrow.toISOString(),
                    dataHoraFim: new Date(tomorrow.getTime() + 3600000).toISOString(),
                    capacidadeMaxima: 10
                }).then(turmaRes => {
                    turmaId = turmaRes.body.id;
                });
            });
        });

        // Criar Aluno
        cy.apiCreateUser({
            nome: 'Aluno Nivel Teste',
            email: alunoEmail,
            senha: 'Aluno@123!',
            role: 'Aluno'
        }).then(res => {
            alunoUserId = res.body.id;
            createdUserIds.push(alunoUserId);
        });
    });

    after(() => {
        createdUserIds.forEach(id => cy.apiDeleteUser(id));
    });

    it('Aluno deve ver seu próprio nível pedagógico como Iniciante', () => {
        cy.clearLocalStorage();
        cy.apiLogin(alunoEmail, 'Aluno@123!');
        cy.visit('/dashboard.html');

        cy.get('[data-section="perfil"]').click();
        cy.get('#perfilNivelPedagogico').should('be.visible').should('have.value', 'Iniciante').should('have.attr', 'readonly');
    });

    it('Professor deve alterar o nível do aluno pelo atalho da tabela', () => {
        cy.clearLocalStorage();
        cy.apiLogin(profEmail, 'Prof@123!');
        cy.visit('/dashboard.html');

        // Acessar aba de Alunos
        cy.get('[data-section="usuarios"]').should('contain.text', 'Alunos').click();
        cy.get('#usuariosBody').should('contain.text', 'Aluno Nivel Teste');

        // Clicar em Nível para o aluno
        cy.get('#usuariosBody').contains('tr', alunoEmail).find('button').contains('Nível').click();
        cy.get('#nivelModal').should('be.visible');
        cy.wait(300);

        // Mudar para Intermediário
        cy.get('#alunoNivelSelect').select('Intermediário');
        cy.intercept('PUT', '**/api/usuarios/*/nivel').as('updateNivelTabela');
        cy.get('#nivelForm').submit();

        cy.wait('@updateNivelTabela').its('response.statusCode').should('eq', 200);
        cy.get('#appToast').should('contain.text', 'Nível pedagógico atualizado');

        // Verificar na tabela
        cy.get('#usuariosBody').contains('tr', alunoEmail).should('contain.text', 'Intermediário');
    });

    it('Professor deve ver inscritos na turma e poder alterar o nível a partir dali', () => {
        // Primeiro, fazer a reserva do aluno na turma via API
        cy.apiLogin(alunoEmail, 'Aluno@123!').then(aluno => {
            cy.request({
                method: 'POST',
                url: '/api/reservas',
                headers: { Authorization: `Bearer ${aluno.token}` },
                body: { turmaId }
            });
        });

        // Agora login como Professor
        cy.clearLocalStorage();
        cy.apiLogin(profEmail, 'Prof@123!');
        cy.intercept('GET', '/api/turmas').as('loadTurmas');
        cy.visit('/dashboard.html');

        // Clicar na turma no calendário
        cy.wait('@loadTurmas');
        cy.get('.fc-event').contains(`Turma Nivel E2E ${ts}`).click();

        // Verificar se exibe a seção de inscritos e o Aluno
        cy.get('#eventoModal').should('be.visible');
        cy.get('#eventoModalBody').should('contain.text', 'Alunos Inscritos (1)');
        cy.get('#eventoModalBody').should('contain.text', 'Aluno Nivel Teste');
        cy.get('#eventoModalBody').should('contain.text', 'Intermediário');

        // Clicar em alterar
        cy.get('#eventoModalBody').contains('button', 'Alterar').click();
        cy.get('#nivelModal').should('be.visible');
        cy.wait(300);

        // Mudar para Avançado
        cy.get('#alunoNivelSelect').select('Avançado');
        cy.intercept('PUT', '**/api/usuarios/*/nivel').as('updateNivelCalendario');
        cy.get('#nivelForm').submit();

        cy.wait('@updateNivelCalendario').its('response.statusCode').should('eq', 200);
        cy.get('#appToast').should('contain.text', 'Nível pedagógico atualizado');
    });

    it('Admin deve ver e alterar o nível do aluno pelo atalho e pelo modal completo', () => {
        cy.clearLocalStorage();
        cy.apiLogin('admin@natacao.com', 'Admin@123');
        cy.visit('/dashboard.html');

        cy.get('[data-section="usuarios"]').should('contain.text', 'Usuários').click();
        cy.get('#usuariosBody').should('contain.text', 'Aluno Nivel Teste');

        // Verificar nível atual (Avançado)
        cy.get('#usuariosBody').contains('tr', alunoEmail).should('contain.text', 'Avançado');

        // 1. Alterar pelo atalho rápido para Alta Performance
        cy.get('#usuariosBody').contains('tr', alunoEmail).find('button[title="Alterar Nível"]').click();
        cy.get('#nivelModal').should('be.visible');
        cy.wait(300);
        cy.get('#alunoNivelSelect').select('Alta Performance');
        cy.intercept('PUT', '**/api/usuarios/*/nivel').as('adminUpdateNivelQuick');
        cy.get('#nivelForm').submit();
        cy.wait('@adminUpdateNivelQuick').its('response.statusCode').should('eq', 200);

        cy.get('#usuariosBody').contains('tr', alunoEmail).should('contain.text', 'Alta Performance');

        // Aguardar a transição de fechamento do modal anterior terminar
        cy.wait(500);

        // 2. Alterar pelo modal completo de edição para Iniciante
        cy.get('#usuariosBody').contains('tr', alunoEmail).find('.btn-action.editar').click();
        cy.get('#usuarioModal').should('be.visible');
        cy.get('#usuarioNivelPedagogico').should('have.value', 'Alta Performance').select('Iniciante');
        
        cy.intercept('PUT', `/api/usuarios/${alunoUserId}`).as('adminUpdateUserFull');
        cy.get('#usuarioForm').submit();
        cy.wait('@adminUpdateUserFull').its('response.statusCode').should('eq', 200);

        cy.get('#usuariosBody').contains('tr', alunoEmail).should('contain.text', 'Iniciante');
    });
});
