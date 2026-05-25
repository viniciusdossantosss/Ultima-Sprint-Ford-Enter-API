/// <reference types="cypress" />

describe('02 - Gestão de Usuários (Admin)', () => {
    let createdUserIds = [];

    beforeEach(() => {
        cy.clearLocalStorage();
        cy.apiLogin('admin@natacao.com', 'Admin@123');
        cy.visit('/dashboard.html');
    });

    afterEach(() => {
        // Cleanup: remover usuários criados durante os testes
        createdUserIds.forEach(id => cy.apiDeleteUser(id));
        createdUserIds = [];
    });

    it('Deve exibir a seção de Gestão de Usuários para Admin', () => {
        cy.get('[data-section="usuarios"]').should('be.visible').click();
        cy.get('#sectionUsuarios').should('be.visible');
        cy.get('#usuariosTable').should('be.visible');
    });

    it('Deve criar um novo Professor', () => {
        cy.get('[data-section="usuarios"]').click();
        cy.get('#btnNovoUsuario').click();
        cy.get('#usuarioModal').should('be.visible');

        cy.get('#usuarioNome').type('Prof. Cypress Test');
        cy.get('#usuarioEmail').type(`prof.cypress.${Date.now()}@test.com`);
        cy.get('#usuarioSenha').type('Prof@123!');
        cy.get('#usuarioRole').select('Professor');

        cy.intercept('POST', '/api/usuarios').as('createUser');
        cy.get('#btnSaveUsuario').click();
        cy.wait('@createUser').then(interception => {
            expect(interception.response.statusCode).to.eq(201);
            createdUserIds.push(interception.response.body.id);
        });

        cy.get('#appToast').should('be.visible');
    });

    it('Deve criar um novo Aluno', () => {
        cy.get('[data-section="usuarios"]').click();
        cy.get('#btnNovoUsuario').click();

        cy.get('#usuarioNome').type('Aluno Cypress Test');
        cy.get('#usuarioEmail').type(`aluno.cypress.${Date.now()}@test.com`);
        cy.get('#usuarioSenha').type('Aluno@123!');
        cy.get('#usuarioRole').select('Aluno');

        // Preencher novos campos obrigatórios
        cy.get('#usuarioDataNascimento').type('2000-01-01');
        cy.get('#usuarioTelefone').type('(11) 99999-9999');

        cy.intercept('POST', '/api/usuarios').as('createUser');
        cy.get('#btnSaveUsuario').click();
        cy.wait('@createUser').then(interception => {
            expect(interception.response.statusCode).to.eq(201);
            createdUserIds.push(interception.response.body.id);
        });
    });

    it('Deve exigir e preencher dados do responsável se o Aluno for menor de idade', () => {
        cy.get('[data-section="usuarios"]').click();
        cy.get('#btnNovoUsuario').click();

        cy.get('#usuarioNome').type('Aluno Menor Test');
        cy.get('#usuarioEmail').type(`menor.cypress.${Date.now()}@test.com`);
        cy.get('#usuarioSenha').type('Aluno@123!');
        cy.get('#usuarioRole').select('Aluno');
        
        // Define data de nascimento para menor de idade (ex: 10 anos atrás)
        const anoMenor = new Date().getFullYear() - 10;
        cy.get('#usuarioDataNascimento').type(`${anoMenor}-01-01`);
        
        // Grupo do responsável deve estar visível
        cy.get('#grupoResponsavel').should('be.visible');
        
        cy.get('#usuarioTelefone').type('(11) 98888-8888');
        cy.get('#usuarioNomeResponsavel').type('Responsável Cypress');
        cy.get('#usuarioTelefoneResponsavel').type('(11) 97777-7777');
        cy.get('#usuarioDocSaude').check();
        cy.get('#usuarioProblemasSaude').type('Asma leve');

        cy.intercept('POST', '/api/usuarios').as('createUserMinor');
        cy.get('#btnSaveUsuario').click();
        cy.wait('@createUserMinor').then(interception => {
            expect(interception.response.statusCode).to.eq(201);
            createdUserIds.push(interception.response.body.id);
            expect(interception.response.body.nomeResponsavel).to.eq('Responsável Cypress');
            expect(interception.response.body.modalidadeSugerida).to.eq('Infantil');
        });
    });

    it('Deve listar usuários na tabela', () => {
        cy.get('[data-section="usuarios"]').click();
        cy.get('#usuariosBody tr').should('have.length.greaterThan', 0);
        // Deve ter pelo menos o Admin
        cy.get('#usuariosBody').should('contain.text', 'admin@natacao.com');
    });

    it('Deve deletar um usuário', () => {
        // Criar um usuário para deletar
        const email = `delete.test.${Date.now()}@test.com`;
        cy.apiCreateUser({ nome: 'Delete Test', email, senha: 'Delete@123!', role: 'Aluno' }).then(res => {
            const userId = res.body.id;
            
            // Recarregar a página para que o frontend busque a lista atualizada contendo o novo usuário
            cy.reload();
            cy.get('[data-section="usuarios"]').click();

            // Esperar a tabela carregar
            cy.get('#usuariosBody').should('contain.text', email);

            cy.intercept('DELETE', `/api/usuarios/${userId}`).as('deleteUser');
            cy.on('window:confirm', () => true);

            // Encontrar e clicar no botão deletar
            cy.get('#usuariosBody').contains('tr', email).find('.btn-action.deletar').click();
            cy.wait('@deleteUser').its('response.statusCode').should('eq', 204);
        });
    });

    it('NÃO deve exibir seção de usuários para Professor', () => {
        const email = `prof.visibility.${Date.now()}@test.com`;
        cy.apiCreateUser({ nome: 'Prof Visibility', email, senha: 'Prof@123!', role: 'Professor' }).then(res => {
            createdUserIds.push(res.body.id);
            cy.clearLocalStorage();
            cy.apiLogin(email, 'Prof@123!');
            cy.visit('/dashboard.html');
            cy.get('[data-section="usuarios"]').should('not.be.visible');
        });
    });
});
