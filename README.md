# 🏊 AquaSchedule — Sistema de Agendamento e Controle para Aulas de Natação

---

## 1. Descrição do Projeto

O **AquaSchedule** é uma API RESTful full-stack para gerenciamento de aulas de natação, construída em **ASP.NET Core 8**. O sistema permite que **professores** criem e gerenciem turmas com horários fixos semanais e que **alunos** realizem reservas nessas turmas, com controle automático de capacidade e detecção de conflitos de horário.

A aplicação segue uma **arquitetura de camadas** (Models → DTOs → Repositories → Services → Controllers), respeitando os princípios **SOLID** e o padrão **Repository**. O frontend responsivo é servido como arquivos estáticos diretamente pela API via a pasta `wwwroot`.

### Funcionalidades Principais

- ✅ Autenticação JWT com registro e login
- ✅ Criptografia BCrypt para senhas
- ✅ CRUD completo de Turmas (Professor)
- ✅ Agendamento e cancelamento de Reservas (Aluno)
- ✅ Controle de capacidade com vagas disponíveis em tempo real
- ✅ Detecção de conflito de horário (algoritmo de sobreposição de intervalos)
- ✅ Autorização por Roles (Professor / Aluno)
- ✅ Frontend responsivo com Bootstrap 5 e tema dark
- ✅ Swagger UI para documentação interativa da API
- ✅ Tratamento global de exceções sem vazamento de stack traces
- ✅ Testes unitários cobrindo regras de negócio (xUnit + Moq)

---

## 2. Tecnologias Utilizadas

- **Linguagem:** C# 12
- **Framework:** ASP.NET Core 8.0
- **Banco de Dados:** MySQL 8.0+ (via Pomelo.EntityFrameworkCore.MySql 8.0.2)
- **ORM:** Entity Framework Core 8.0
- **Segurança:**
  - Autenticação: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer` 8.0)
  - Hash de senhas: BCrypt.Net-Next 4.0
- **Documentação de API:** Swagger / Swashbuckle 6.9
- **Mapeamento de objetos:** AutoMapper 13.0
- **Frontend:** HTML5, CSS3, JavaScript (ES6+), Bootstrap 5.3
- **Testes:** xUnit 2.9 + Moq 4.20
- **Containerização:** Docker (multi-stage build com .NET SDK 8.0)

---

## 3. Instruções de Execução

Para rodar o projeto localmente, siga os passos abaixo:

### Pré-requisitos

- [.NET SDK 8.0+](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL Server 8.0+](https://dev.mysql.com/downloads/)
- (Opcional) JetBrains Rider, Visual Studio ou VS Code

### Passo a passo

1. **Clone o repositório e acesse a pasta do projeto:**
   ```bash
   git clone https://github.com/seu-usuario/Ultima-Sprint-Ford-Enter-API.git
   cd Ultima-Sprint-Ford-Enter-API
   ```

2. **Restaure os pacotes NuGet:**
   ```bash
   dotnet restore
   ```

3. **Configure a connection string do banco de dados:**
   Edite o arquivo `NatacaoAPI/appsettings.json` e atualize a `ConnectionString` com as credenciais do seu MySQL:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Port=3306;Database=natacao_db;User=root;Password=SUA_SENHA;"
   }
   ```

4. **Crie o banco de dados via Migrations:**
   ```bash
   dotnet ef migrations add InitialCreate --project NatacaoAPI
   dotnet ef database update --project NatacaoAPI
   ```

5. **Execute a aplicação:**
   ```bash
   dotnet run --project NatacaoAPI
   ```
   Após iniciar, acesse:
   | Recurso   | URL                              |
   |-----------|----------------------------------|
   | Frontend  | http://localhost:5000             |
   | Swagger   | http://localhost:5000/swagger     |
   | API       | http://localhost:5000/api/...     |
   | Health    | http://localhost:5000/health      |

### Executar os testes

```bash
dotnet test
```

### Via Docker (opcional)

```bash
docker build -t aquaschedule .
docker run -p 8080:8080 aquaschedule
```

---

## 4. Endpoints da API

Abaixo estão os principais endpoints disponíveis no sistema:

### 🔓 Autenticação (Público)

| Método | Rota                 | Descrição                                    | Status Codes           |
|--------|----------------------|----------------------------------------------|------------------------|
| `POST` | `/api/auth/register` | Registra um novo usuário (Aluno ou Professor) | `201` · `400`          |
| `POST` | `/api/auth/login`    | Realiza login e retorna token JWT             | `200` · `401`          |

### 📚 Turmas (Autenticado)

| Método   | Rota               | Descrição                                  | Acesso              | Status Codes           |
|----------|--------------------|--------------------------------------------|----------------------|------------------------|
| `GET`    | `/api/turmas`      | Lista todas as turmas com vagas disponíveis | Qualquer autenticado | `200`                  |
| `GET`    | `/api/turmas/{id}` | Retorna detalhes de uma turma              | Qualquer autenticado | `200` · `404`          |
| `POST`   | `/api/turmas`      | Cria uma nova turma                        | Professor            | `201` · `400` · `403`  |
| `PUT`    | `/api/turmas/{id}` | Atualiza uma turma existente               | Professor            | `200` · `404` · `403`  |
| `DELETE` | `/api/turmas/{id}` | Remove uma turma                           | Professor            | `204` · `404` · `403`  |

### 📅 Reservas (Autenticado)

| Método   | Rota                 | Descrição                                       | Acesso           | Status Codes           |
|----------|----------------------|-------------------------------------------------|------------------|------------------------|
| `GET`    | `/api/reservas`      | Lista reservas (Professor=todas, Aluno=próprias) | Qualquer autenticado | `200`              |
| `GET`    | `/api/reservas/{id}` | Detalhes de uma reserva específica               | Dono ou Professor | `200` · `404` · `403` |
| `POST`   | `/api/reservas`      | Cria reserva (valida capacidade e conflitos)     | Aluno            | `201` · `400`          |
| `DELETE` | `/api/reservas/{id}` | Cancela uma reserva (soft delete)                | Aluno (dono)     | `204` · `404` · `403`  |

> 💡 **Dica:** Todos os endpoints autenticados requerem o header `Authorization: Bearer {seu_token_jwt}`. Utilize o Swagger UI em `/swagger` para testar interativamente.

---

## 5. Regras de Negócio

| Código   | Regra                  | Descrição                                                                                                |
|----------|------------------------|----------------------------------------------------------------------------------------------------------|
| **RF001** | Controle de Capacidade | O agendamento falha com `400 Bad Request` se a capacidade máxima de alunos daquela turma for excedida.   |
| **RF002** | Conflito de Horário    | O agendamento falha com `400 Bad Request` se o aluno já possuir reserva ativa com sobreposição de horário.|
| **RF003** | Perfil Professor       | Professores podem criar, editar e deletar turmas, além de visualizar todas as reservas e alunos.          |
| **RF004** | Perfil Aluno           | Alunos podem listar turmas, criar reservas para si e cancelar apenas suas próprias reservas.              |

---

## 6. Estrutura do Projeto

```
Ultima-Sprint-Ford-Enter-API/
├── Dockerfile                # Multi-stage build para containerização
├── NatacaoAPI.sln            # Solution file
│
├── NatacaoAPI/               # Projeto principal (API)
│   ├── Controllers/          # Endpoints HTTP (enxutos, sem lógica de negócio)
│   ├── Data/                 # DbContext com configuração Fluent API
│   ├── DTOs/                 # Data Transfer Objects (Request/Response)
│   │   ├── Auth/
│   │   ├── Turma/
│   │   └── Reserva/
│   ├── Middleware/            # Global Exception Handler
│   ├── Migrations/            # Migrations do EF Core
│   ├── Models/                # Entidades de domínio (Usuario, Turma, Reserva)
│   ├── Profiles/              # Configurações AutoMapper
│   ├── Repositories/          # Padrão Repository (Interface + Implementação EF Core)
│   │   └── Interfaces/
│   ├── Services/              # Regras de negócio (Interface + Implementação)
│   │   └── Interfaces/
│   ├── wwwroot/               # Frontend integrado (HTML/CSS/JS + Bootstrap)
│   ├── Program.cs             # Configuração DI, Auth, Middleware pipeline
│   └── appsettings.json       # Connection string e configuração JWT
│
└── NatacaoAPI.Tests/          # Projeto de testes unitários
    └── Services/
        ├── ReservaServiceTests.cs
        └── TurmaServiceTests.cs
```

---

## 7. Arquitetura de Camadas

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
