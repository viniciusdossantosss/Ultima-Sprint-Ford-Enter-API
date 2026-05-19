# 🏊 AquaSchedule — Sistema de Agendamento e Controle para Aulas de Natação

API RESTful em ASP.NET Core 8 com frontend integrado para gerenciamento de turmas de natação, reservas de alunos e controle de capacidade.

---

## 📋 Documentação Funcional

### 1. Objetivo do Sistema

O AquaSchedule é um sistema de agendamento de aulas de natação que permite a **professores** criarem e gerenciarem turmas com horários fixos semanais, e a **alunos** realizarem reservas nessas turmas. O sistema garante o controle de capacidade máxima por turma e previne conflitos de horário para os alunos.

### 2. Regras de Negócio

| Código | Regra | Descrição |
|--------|-------|-----------|
| **RF001** | Controle de Capacidade | O agendamento (POST de Reserva) deve falhar com `400 Bad Request` se a capacidade máxima de alunos daquela turma for excedida. |
| **RF002** | Conflito de Horário | O agendamento deve falhar com `400 Bad Request` se o aluno já possuir uma reserva ativa em turma com o mesmo dia da semana e sobreposição de horário. |
| **RF003** | Perfil Professor | Usuários com role `Professor` podem criar, editar e deletar turmas, além de visualizar todas as reservas e alunos do sistema. |
| **RF004** | Perfil Aluno | Usuários com role `Aluno` podem apenas listar turmas disponíveis, criar reservas para si e cancelar suas próprias reservas. Tentativas de cancelar reservas de outros alunos resultam em `401 Unauthorized`. |

### 3. Funcionalidades Principais

- ✅ **Autenticação JWT** com registro e login
- ✅ **Criptografia BCrypt** para senhas
- ✅ **CRUD completo** de Turmas (Professor)
- ✅ **Agendamento e cancelamento** de Reservas (Aluno)
- ✅ **Controle de capacidade** com vagas disponíveis em tempo real
- ✅ **Detecção de conflito de horário** com algoritmo de sobreposição de intervalos
- ✅ **Autorização por Roles** (Professor / Aluno)
- ✅ **Frontend responsivo** com Bootstrap 5 e tema dark
- ✅ **Swagger UI** para documentação interativa da API
- ✅ **Tratamento global de exceções** sem vazamento de stack traces
- ✅ **Testes unitários** cobrindo regras de negócio

---

## 📖 Documentação Técnica

### 1. Descrição do Projeto

Aplicação web full-stack construída em arquitetura de camadas (Models → DTOs → Repositories → Services → Controllers) seguindo os princípios SOLID e padrão Repository. O frontend é servido como arquivos estáticos na pasta `wwwroot` da própria API.

### 2. Tecnologias Utilizadas

| Categoria | Tecnologia | Versão |
|-----------|-----------|--------|
| **Linguagem** | C# | 12 |
| **Framework** | ASP.NET Core | 8.0 |
| **ORM** | Entity Framework Core | 8.0 |
| **Banco de Dados** | MySQL | 8.0+ |
| **Provider MySQL** | Pomelo.EntityFrameworkCore.MySql | 8.0.2 |
| **Mapeamento** | AutoMapper | 12.0 |
| **Segurança - Auth** | JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer) | 8.0 |
| **Segurança - Hash** | BCrypt.Net-Next | 4.0 |
| **Documentação API** | Swagger / Swashbuckle | 6.9 |
| **Frontend** | HTML5, CSS3, JavaScript (ES6+) | - |
| **UI Framework** | Bootstrap | 5.3 |
| **Testes** | xUnit | 2.9 |
| **Mocking** | Moq | 4.20 |

### 3. Instruções de Execução

#### Pré-requisitos
- .NET SDK 8.0+
- MySQL Server 8.0+
- (Opcional) JetBrains Rider ou Visual Studio

#### Passo a passo via CLI

```bash
# 1. Clonar ou navegar até a pasta do projeto
cd WEB-API_ultima_sprint

# 2. Restaurar pacotes NuGet
dotnet restore

# 3. Configurar o banco de dados
#    Edite o arquivo NatacaoAPI/appsettings.json e atualize a ConnectionString:
#    "Server=localhost;Port=3306;Database=natacao_db;User=root;Password=SUA_SENHA;"

# 4. Criar o banco via Migrations
dotnet ef migrations add InitialCreate --project NatacaoAPI
dotnet ef database update --project NatacaoAPI

# 5. Executar a API
dotnet run --project NatacaoAPI

# 6. Acessar:
#    Frontend: http://localhost:5000
#    Swagger:  http://localhost:5000/swagger
#    API:      http://localhost:5000/api/...

# 7. Executar os testes
dotnet test
```

#### Via JetBrains Rider
1. Abra o arquivo `NatacaoAPI.sln`
2. Configure a connection string no `appsettings.json`
3. Execute as Migrations pelo terminal integrado
4. Clique em **Run** no projeto `NatacaoAPI`

### 4. Endpoints da API

#### Autenticação (Público)

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/auth/register` | Registra um novo usuário (Aluno ou Professor) |
| `POST` | `/api/auth/login` | Realiza login e retorna token JWT |

#### Turmas (Autenticado)

| Método | Rota | Descrição | Acesso |
|--------|------|-----------|--------|
| `GET` | `/api/turmas` | Lista todas as turmas com vagas disponíveis | Qualquer autenticado |
| `GET` | `/api/turmas/{id}` | Retorna detalhes de uma turma | Qualquer autenticado |
| `POST` | `/api/turmas` | Cria uma nova turma | Professor |
| `PUT` | `/api/turmas/{id}` | Atualiza uma turma existente | Professor |
| `DELETE` | `/api/turmas/{id}` | Remove uma turma | Professor |

#### Reservas (Autenticado)

| Método | Rota | Descrição | Acesso |
|--------|------|-----------|--------|
| `GET` | `/api/reservas` | Lista reservas (Professor=todas, Aluno=próprias) | Qualquer autenticado |
| `GET` | `/api/reservas/{id}` | Detalhes de uma reserva específica | Dono ou Professor |
| `POST` | `/api/reservas` | Cria uma nova reserva (valida RF001 e RF002) | Aluno |
| `DELETE` | `/api/reservas/{id}` | Cancela uma reserva (soft delete) | Aluno (dono) |

### 5. Estrutura do Projeto

```
NatacaoAPI/
├── Controllers/          # Endpoints HTTP (enxutos, sem lógica de negócio)
├── Data/                 # DbContext com configuração Fluent API
├── DTOs/                 # Data Transfer Objects (Request/Response separados)
│   ├── Auth/
│   ├── Turma/
│   └── Reserva/
├── Middleware/            # Global Exception Handler
├── Models/               # Entidades de domínio ricas
├── Profiles/             # Configurações AutoMapper
├── Repositories/         # Padrão Repository (Interface + Implementação EF Core)
│   └── Interfaces/
├── Services/             # Regras de negócio (Interface + Implementação)
│   └── Interfaces/
├── wwwroot/              # Frontend integrado (HTML/CSS/JS + Bootstrap)
├── Program.cs            # Configuração DI, Auth, Middleware pipeline
└── appsettings.json      # Connection string e configuração JWT

NatacaoAPI.Tests/
└── Services/             # Testes unitários xUnit + Moq
    ├── ReservaServiceTests.cs
    └── TurmaServiceTests.cs
```

### 6. Arquitetura de Camadas

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Controller  │ ──▶ │   Service    │ ──▶ │  Repository  │ ──▶ │  DbContext   │
│  (HTTP)      │     │  (Negócio)   │     │  (Dados)     │     │  (EF Core)   │
└─────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
       │                    │
       ▼                    ▼
   ┌───────┐          ┌──────────┐
   │  DTO  │          │  Model   │
   │ (API) │  ◀──────▶│ (Domínio)│
   └───────┘          └──────────┘
        AutoMapper
```

---

## 📝 Licença

Este projeto foi desenvolvido para fins acadêmicos.
