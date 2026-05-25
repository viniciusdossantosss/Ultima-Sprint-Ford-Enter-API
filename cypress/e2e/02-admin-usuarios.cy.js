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

    it('Deve buscar aluno por nome', () => {
        cy.get('[data-section="usuarios"]').click();
        
        // Criar um usuário específico para buscar
        const nomeBusca = `Busca_${Date.now()}`;
        const emailBusca = `${nomeBusca.toLowerCase()}@test.com`;
        cy.apiCreateUser({ nome: nomeBusca, email: emailBusca, role: 'Aluno' }).then(res => {
            createdUserIds.push(res.body.id);
            cy.reload();
            cy.get('[data-section="usuarios"]').click();
            
            // Digitar na busca
            cy.get('#buscaUsuario').type(nomeBusca);
            
            // A tabela deve exibir o usuário e esconder outros que não contêm o nome
            cy.get('#usuariosBody').should('contain.text', nomeBusca);
            cy.get('#usuariosBody tr').should('have.length', 1);
        });
    });

    it('Deve editar dados de um Aluno existente', () => {
        const email = `editar.cypress.${Date.now()}@test.com`;
        cy.apiCreateUser({ nome: 'Aluno Para Editar', email, role: 'Aluno' }).then(res => {
            const userId = res.body.id;
            createdUserIds.push(userId);
            
            cy.reload();
            cy.get('[data-section="usuarios"]').click();
            cy.get('#usuariosBody').should('contain.text', email);

            // Clicar no botão de editar
            cy.get('#usuariosBody').contains('tr', email).find('.btn-action.editar').click();
            cy.get('#usuarioModal').should('be.visible');
            
            // O modal deve estar com o campo de perfil desabilitado
            cy.get('#usuarioRole').should('be.disabled');

            // Aguardar a animação do modal terminar para que o Bootstrap não roube o foco
            cy.wait(500);

            // Garantir que os dados do usuário foram carregados no formulário
            cy.get('#usuarioNome').should('have.value', 'Aluno Para Editar');

            // Alterar telefone e saúde
            cy.get('#usuarioNome').clear().type('Aluno Editado Cypress').should('have.value', 'Aluno Editado Cypress');
            cy.get('#usuarioTelefone').clear().type('(11) 98765-4321').should('have.value', '(11) 98765-4321');
            cy.get('#usuarioDocSaude').check();

            cy.intercept('PUT', `/api/usuarios/${userId}`).as('updateUser');
            cy.get('#btnSaveUsuario').click();
            cy.wait('@updateUser').its('response.statusCode').should('eq', 200);

            // Verificar se os dados alterados aparecem na listagem
            cy.get('#usuariosBody').should('contain.text', 'Aluno Editado Cypress');
            cy.get('#usuariosBody').should('contain.text', '(11) 98765-4321');
            cy.get('#usuariosBody').should('contain.text', 'Doc. OK');
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

    it('Deve exibir seção de Alunos para Professor sem permitir criação', () => {
        const email = `prof.visibility.${Date.now()}@test.com`;
        cy.apiCreateUser({ nome: 'Prof Visibility', email, senha: 'Prof@123!', role: 'Professor' }).then(res => {
            createdUserIds.push(res.body.id);
            cy.clearLocalStorage();
            cy.apiLogin(email, 'Prof@123!');
            cy.visit('/dashboard.html');
            
            // Professor deve ver a aba, mas com o texto "Alunos"
            cy.get('[data-section="usuarios"]').should('be.visible').should('contain.text', 'Alunos').click();
            cy.get('#sectionUsuarios').should('be.visible');
            
            // Professor NÃO deve ver o botão de Novo Usuário
            cy.get('#btnNovoUsuario').should('not.be.visible');
        });
    });
});
