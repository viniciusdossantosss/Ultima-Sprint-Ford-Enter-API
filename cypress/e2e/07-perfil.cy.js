/// <reference types="cypress" />

describe('07 - Perfil do Usuário e Validações de Professor', () => {
    let createdUserIds = [];

    beforeEach(() => {
        cy.clearLocalStorage();
    });

    afterEach(() => {
        // Cleanup users
        createdUserIds.forEach(id => cy.apiDeleteUser(id));
        createdUserIds = [];
    });

    it('Deve permitir ao Aluno atualizar os dados básicos do seu perfil', () => {
        const alunoEmail = `alunoperfil.${Date.now()}@test.com`;
        
        // Criar aluno via API
        cy.apiCreateUser({
            nome: 'Aluno Perfil Teste',
            email: alunoEmail,
            role: 'Aluno',
            dataNascimento: '1995-05-15',
            telefone: '(11) 91111-1111'
        }).then(res => {
            const user = res.body;
            createdUserIds.push(user.id);
            
            // Login via UI
            cy.uiLogin(alunoEmail, 'Aluno@123!');
            
            // Ir para Aba Meu Perfil
            cy.get('[data-section="perfil"]').click();
            cy.get('#sectionPerfil').should('be.visible');
            
            // Verificar se os campos foram preenchidos
            cy.get('#perfilNome').should('have.value', 'Aluno Perfil Teste');
            cy.get('#perfilEmail').should('have.value', alunoEmail);
            cy.get('#perfilTelefone').should('have.value', '(11) 91111-1111');
            cy.get('#perfilDataNascimento').should('have.value', '1995-05-15');
            
            // Atualizar Nome e Telefone
            cy.get('#perfilNome').clear().type('Aluno Perfil Atualizado');
            cy.get('#perfilTelefone').clear().type('(11) 92222-2222');
            
            cy.intercept('PUT', '/api/usuarios/perfil').as('updatePerfil');
            cy.get('#btnSalvarPerfil').click();
            cy.wait('@updatePerfil').its('response.statusCode').should('eq', 200);
            
            cy.get('#appToast').should('contain', 'Perfil atualizado com sucesso!');
            cy.get('#navUserName').should('contain', 'Aluno Perfil Atualizado');
        });
    });

    it('Deve exigir responsável legal se Aluno menor de idade atualizar o perfil', () => {
        const alunoEmail = `alunomenor.${Date.now()}@test.com`;
        
        // Criar aluno via API
        cy.apiCreateUser({
            nome: 'Aluno Menor Perfil',
            email: alunoEmail,
            role: 'Aluno',
            dataNascimento: '1995-05-15', // Maior inicialmente
            telefone: '(11) 91111-1111'
        }).then(res => {
            const user = res.body;
            createdUserIds.push(user.id);
            
            cy.uiLogin(alunoEmail, 'Aluno@123!');
            cy.get('[data-section="perfil"]').click();
            
            // Mudar data de nascimento para menor de idade
            const anoMenor = new Date().getFullYear() - 12;
            cy.get('#perfilDataNascimento').clear().type(`${anoMenor}-06-15`);
            
            // Deve exibir grupo de responsável
            cy.get('#perfilGrupoResponsavel').should('be.visible');
            
            // Submeter sem preencher responsável deve falhar por validação HTML5 do navegador ou API
            // Preencher campos do responsável
            cy.get('#perfilNomeResponsavel').type('Pai do Aluno');
            cy.get('#perfilTelefoneResponsavel').type('(11) 93333-3333');
            
            cy.intercept('PUT', '/api/usuarios/perfil').as('updatePerfilMenor');
            cy.get('#btnSalvarPerfil').click();
            cy.wait('@updatePerfilMenor').its('response.statusCode').should('eq', 200);
            cy.get('#appToast').should('contain', 'Perfil atualizado com sucesso!');
        });
    });

    it('Deve gerenciar troca de senha com validação de segurança', () => {
        const alunoEmail = `alunosenha.${Date.now()}@test.com`;
        
        cy.apiCreateUser({
            nome: 'Aluno Troca Senha',
            email: alunoEmail,
            role: 'Aluno',
            dataNascimento: '1990-01-01',
            telefone: '(11) 99999-9999'
        }).then(res => {
            const user = res.body;
            createdUserIds.push(user.id);
            
            cy.uiLogin(alunoEmail, 'Aluno@123!');
            cy.get('[data-section="perfil"]').click();
            
            // 1. Tentar alterar sem senha atual
            cy.get('#perfilNovaSenha').type('NovaSenha@123');
            cy.get('#btnSalvarPerfil').click();
            cy.get('#appToast').should('contain', 'informar a senha atual');
            
            // Limpar
            cy.get('#perfilNovaSenha').clear();
            
            // 2. Tentar com senha atual incorreta
            cy.get('#perfilSenhaAtual').type('SenhaErrada@123');
            cy.get('#perfilNovaSenha').type('NovaSenha@123');
            cy.get('#btnSalvarPerfil').click();
            cy.get('#appToast').should('contain', 'Senha atual incorreta');
            
            // Limpar
            cy.get('#perfilSenhaAtual').clear();
            cy.get('#perfilNovaSenha').clear();
            
            // 3. Alterar com dados corretos
            cy.get('#perfilSenhaAtual').type('Aluno@123!');
            cy.get('#perfilNovaSenha').type('NovaSenha@123');
            
            cy.intercept('PUT', '/api/usuarios/perfil').as('updateSenha');
            cy.get('#btnSalvarPerfil').click();
            cy.wait('@updateSenha').its('response.statusCode').should('eq', 200);
            
            cy.get('#appToast').should('contain', 'Perfil atualizado com sucesso!');
            
            // 4. Testar logout e login com nova senha
            cy.get('#btnLogout').click();
            cy.uiLogin(alunoEmail, 'NovaSenha@123');
        });
    });

    it('Deve validar aptidão e CREF ao alocar professor em turmas', () => {
        // Criar Professor sem aptidão para Hidroginástica
        const profEmail = `prof.teste.${Date.now()}@test.com`;
        
        cy.apiCreateUser({
            nome: 'Prof Cypress Apto',
            email: profEmail,
            role: 'Professor',
            cref: '654321-G/SP',
            crefAtivo: true,
            aptoBebes: true,
            aptoInfantil: true,
            aptoAdulto: true,
            aptoAltaPerformance: false,
            aptoHidroginastica: false, // Inapto para Hidroginástica
            aptoPcd: false
        }).then(res => {
            const prof = res.body;
            createdUserIds.push(prof.id);
            
            // Fazer login como Admin para criar a turma alocando o professor
            cy.apiLogin('admin@natacao.com', 'Admin@123');
            cy.visit('/dashboard.html');
            
            // Clicar no dia 15 do mês atual no calendário para abrir modal de Nova Turma
            cy.get('.fc-daygrid-day').eq(15).click();
            cy.get('#turmaModal').should('be.visible');
            
            // Preencher campos da turma
            cy.get('#turmaNome').type('Turma Hidro E2E');
            cy.get('#turmaModalidade').select('Hidroginastica');
            
            // Selecionar o professor recém-criado
            cy.get('#turmaProfessorId').invoke('val', prof.id.toString()).trigger('change');
            
            cy.intercept('POST', '/api/turmas').as('createTurmaFail');
            cy.get('#btnSaveTurma').click();
            
            // Deve falhar pois o professor não tem a aptidão necessária
            cy.wait('@createTurmaFail').then(interception => {
                expect(interception.response.statusCode).to.eq(400);
                expect(interception.response.body.message).to.contain('não está apto');
            });
            
            // Fechar modal de Turma
            cy.get('#turmaModal').find('.btn-close').click();
        });
    });

    it('Deve validar se o CREF do professor está ativo ao alocar em turmas', () => {
        // Criar Professor com CREF inativo
        const profEmail = `prof.inativo.${Date.now()}@test.com`;
        
        cy.apiCreateUser({
            nome: 'Prof CREF Inativo',
            email: profEmail,
            role: 'Professor',
            cref: '999999-G/SP',
            crefAtivo: false, // Inativo
            aptoBebes: true,
            aptoInfantil: true,
            aptoAdulto: true,
            aptoHidroginastica: true
        }).then(res => {
            const prof = res.body;
            createdUserIds.push(prof.id);
            
            cy.apiLogin('admin@natacao.com', 'Admin@123');
            cy.visit('/dashboard.html');
            
            cy.get('.fc-daygrid-day').eq(16).click();
            cy.get('#turmaModal').should('be.visible');
            
            cy.get('#turmaNome').type('Turma Adulto E2E');
            cy.get('#turmaModalidade').select('Adulto');
            cy.get('#turmaProfessorId').invoke('val', prof.id.toString()).trigger('change');
            
            cy.intercept('POST', '/api/turmas').as('createTurmaFailCref');
            cy.get('#btnSaveTurma').click();
            
            // Deve falhar por CREF inativo
            cy.wait('@createTurmaFailCref').then(interception => {
                expect(interception.response.statusCode).to.eq(400);
                expect(interception.response.body.message).to.contain('CREF preenchido e ativo');
            });
        });
    });
});
