/// <reference types="cypress" />

describe('01 - Autenticação', () => {
    beforeEach(() => {
        cy.clearLocalStorage();
    });

    it('Deve exibir o formulário de login na página inicial', () => {
        cy.visit('/');
        cy.get('#loginForm').should('be.visible');
        cy.get('#loginEmail').should('be.visible');
        cy.get('#loginSenha').should('be.visible');
        cy.get('#btnLogin').should('be.visible');
    });

    it('NÃO deve exibir formulário de registro', () => {
        cy.visit('/');
        cy.get('#registerForm').should('not.exist');
        cy.get('#registerTab').should('not.exist');
    });

    it('Deve fazer login como Admin e redirecionar para dashboard', () => {
        cy.fixture('users').then((users) => {
            cy.visit('/');
            cy.get('#loginEmail').type(users.admin.email);
            cy.get('#loginSenha').type(users.admin.senha);
            cy.get('#btnLogin').click();
            cy.url().should('include', 'dashboard');
            cy.get('#navUserName').should('contain.text', 'Administrador');
        });
    });

    it('Deve fazer login como Professor e redirecionar para dashboard', () => {
        cy.fixture('users').then((users) => {
            cy.visit('/');
            cy.get('#loginEmail').type(users.professor.email);
            cy.get('#loginSenha').type(users.professor.senha);
            cy.get('#btnLogin').click();
            cy.url().should('include', 'dashboard');
            cy.get('#navUserName').should('contain.text', users.professor.nome);
        });
    });

    it('Deve fazer login como Aluno e redirecionar para dashboard', () => {
        cy.fixture('users').then((users) => {
            cy.visit('/');
            cy.get('#loginEmail').type(users.aluno.email);
            cy.get('#loginSenha').type(users.aluno.senha);
            cy.get('#btnLogin').click();
            cy.url().should('include', 'dashboard');
            cy.get('#navUserName').should('contain.text', users.aluno.nome);
        });
    });

    it('Deve exibir erro para credenciais inválidas', () => {
        cy.visit('/');
        cy.get('#loginEmail').type('naoexiste@test.com');
        cy.get('#loginSenha').type('senhaerrada');
        cy.get('#btnLogin').click();
        cy.get('#authAlert').should('be.visible');
    });

    it('Deve fazer logout e redirecionar para login', () => {
        cy.uiLogin('admin@natacao.com', 'Admin@123');
        cy.get('#btnLogout').click();
        cy.url().should('not.include', 'dashboard');
    });

    it('Deve exibir formulário de recuperação de senha', () => {
        cy.visit('/');
        cy.get('#forgotPasswordLink').click();
        cy.get('#forgotView').should('be.visible');
        cy.get('#loginView').should('not.be.visible');
        cy.get('#forgotEmail').should('be.visible');
    });

    it('Deve redirecionar para dashboard se já estiver logado', () => {
        cy.apiLogin('admin@natacao.com', 'Admin@123');
        cy.visit('/');
        cy.url().should('include', 'dashboard');
    });
});
