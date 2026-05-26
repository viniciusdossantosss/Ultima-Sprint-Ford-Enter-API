# 📋 Documentação Funcional — AquaSchedule

---

## 1. Objetivo do Sistema

O **AquaSchedule** é um sistema de agendamento de aulas de natação que permite a **professores** criarem e gerenciarem turmas com horários fixos, e a **alunos** realizarem reservas nessas turmas.

O sistema garante:
- **Controle de capacidade máxima** por turma, impedindo overbooking.
- **Prevenção de conflitos de horário** para os alunos, utilizando um algoritmo de sobreposição de intervalos.
- **Segregação de acesso** entre os perfis Professor e Aluno, garantindo que cada tipo de usuário execute apenas as ações permitidas ao seu papel.

---

## 2. Regras de Negócio

### RF001 — Controle de Capacidade

O agendamento de uma reserva (`POST /api/reservas`) deve falhar com **`400 Bad Request`** se a quantidade de reservas ativas na turma já tiver atingido a capacidade máxima definida.

- **Validação:** Compara a contagem de reservas com `Status = Ativa` contra o campo `CapacidadeMaxima` da turma.
- **Mensagem retornada:** `"RF001: A turma '{nome}' já atingiu a capacidade máxima de {n} alunos."`
- **Onde é validada:** [ReservaService.cs — linha 61](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Services/ReservaService.cs#L60-L64)

---

### RF002 — Conflito de Horário

O agendamento de uma reserva deve falhar com **`400 Bad Request`** se o aluno já possuir uma reserva ativa em outra turma cujo intervalo de horário se sobreponha ao da turma solicitada.

- **Validação:** Utiliza o método `AlunoHasConflictAsync` que verifica sobreposição de intervalos (`DataHoraInicio` / `DataHoraFim`) entre a turma desejada e as turmas já reservadas pelo aluno.
- **Algoritmo de sobreposição:** `InicioA < FimB && InicioB < FimA` — se ambas as condições forem verdadeiras, há conflito.
- **Mensagem retornada:** `"RF002: Você já possui uma aula agendada que conflita com este horário ({início} - {fim})."`
- **Onde é validada:** [ReservaService.cs — linha 72](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Services/ReservaService.cs#L71-L75) e [ReservaRepository.cs — linha 64](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Repositories/ReservaRepository.cs#L64-L73)

---

### RF003 — Perfil Professor

Usuários com role **`Professor`** possuem as seguintes permissões:

| Ação | Endpoint | Descrição |
|------|----------|-----------|
| Criar turma | `POST /api/turmas` | Cria uma nova turma. O `ProfessorId` é extraído automaticamente do token JWT. |
| Editar turma | `PUT /api/turmas/{id}` | Atualiza nome, descrição, modalidade, horários e capacidade de uma turma. |
| Deletar turma | `DELETE /api/turmas/{id}` | Remove uma turma do sistema. |
| Listar todas as reservas | `GET /api/reservas` | Visualiza todas as reservas de todos os alunos do sistema. |
| Visualizar detalhes de reserva | `GET /api/reservas/{id}` | Acessa os detalhes de qualquer reserva. |

- **Onde é controlado:** Atributo `[Authorize(Roles = "Professor")]` nos controllers [TurmasController.cs](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Controllers/TurmasController.cs) e [ReservasController.cs](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Controllers/ReservasController.cs).

---

### RF004 — Perfil Aluno

Usuários com role **`Aluno`** possuem as seguintes permissões:

| Ação | Endpoint | Descrição |
|------|----------|-----------|
| Listar turmas | `GET /api/turmas` | Visualiza todas as turmas disponíveis com vagas em tempo real. |
| Ver detalhes de turma | `GET /api/turmas/{id}` | Visualiza detalhes de uma turma específica. |
| Criar reserva | `POST /api/reservas` | Agenda uma reserva para si (sujeito às validações RF001 e RF002). |
| Listar suas reservas | `GET /api/reservas` | Visualiza apenas suas próprias reservas. |
| Cancelar reserva | `DELETE /api/reservas/{id}` | Cancela (soft delete) apenas suas próprias reservas. |

**Restrições:**
- Tentativas de cancelar reservas de outros alunos resultam em **`401 Unauthorized`** com a mensagem: `"Você não tem permissão para cancelar esta reserva."`
- Tentativas de acessar detalhes de reservas de outros alunos resultam em **`403 Forbidden`**.
- Alunos **não podem** criar, editar ou deletar turmas — essas rotas retornam **`403 Forbidden`**.

- **Onde é controlado:** [ReservaService.cs — linha 99](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Services/ReservaService.cs#L98-L101)

---

### RF005 — Unicidade de E-mail

O sistema não permite o cadastro de dois usuários com o mesmo endereço de e-mail. A unicidade é garantida em **duas camadas**:

- **Camada de aplicação:** O `AuthService` verifica via `EmailExistsAsync` antes de criar o usuário, retornando `400 Bad Request` com a mensagem: `"Este e-mail já está cadastrado."`
- **Camada de banco de dados:** Índice único (`HasIndex(u => u.Email).IsUnique()`) no `AppDbContext`, garantindo integridade mesmo em cenários de concorrência.
- **Onde é validada:** [AuthService.cs — linha 34](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Services/AuthService.cs#L33-L35) e [AppDbContext.cs — linha 30](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Data/AppDbContext.cs#L30)

---

### RF006 — Validação de Senha

A senha informada no registro deve possuir **no mínimo 6 caracteres**. Caso contrário, a requisição é rejeitada com `400 Bad Request`.

- **Validação:** Atributo `[MinLength(6)]` no DTO `RegisterRequestDTO`.
- **Mensagem retornada:** `"A senha deve ter no mínimo 6 caracteres."`
- **Armazenamento:** A senha é armazenada como hash BCrypt (`BCrypt.Net.BCrypt.HashPassword`), nunca em texto plano.
- **Onde é validada:** [RegisterRequestDTO.cs — linha 17](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/DTOs/Auth/RegisterRequestDTO.cs#L16-L18)

---

### RF007 — Role Padrão no Registro

Quando o campo `Role` não é informado no registro, o sistema assume automaticamente o perfil **`Aluno`** como valor padrão. Somente os valores `"Aluno"` e `"Professor"` são aceitos.

- **Validação:** Conversão via `Enum.TryParse<UsuarioRole>` no `AuthService`. Valores inválidos retornam `400 Bad Request`.
- **Mensagem retornada:** `"Role inválida. Use 'Aluno' ou 'Professor'."`
- **Onde é validada:** [AuthService.cs — linha 38](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Services/AuthService.cs#L37-L39) e [RegisterRequestDTO.cs — linha 23](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/DTOs/Auth/RegisterRequestDTO.cs#L23)

---

### RF008 — Reserva Duplicada na Mesma Turma

O sistema impede que um aluno crie **mais de uma reserva ativa** na mesma turma. Tentativas resultam em `400 Bad Request`.

- **Validação:** O método `AlunoJaReservouTurmaAsync` verifica se já existe uma reserva com `Status = Ativa` para o par `(AlunoId, TurmaId)`.
- **Mensagem retornada:** `"Você já possui uma reserva ativa nesta turma."`
- **Onde é validada:** [ReservaService.cs — linha 67](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Services/ReservaService.cs#L67-L69) e [ReservaRepository.cs — linha 75](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Repositories/ReservaRepository.cs#L75-L82)

---

### RF009 — Cancelamento por Soft Delete

O cancelamento de uma reserva **não remove o registro do banco de dados**. Em vez disso, o campo `Status` é alterado de `Ativa` para `Cancelada`, preservando o histórico completo de agendamentos.

- **Comportamento:** Reservas canceladas não são contabilizadas no cálculo de vagas disponíveis (RF001) nem na detecção de conflitos de horário (RF002).
- **Restrição adicional:** Tentativas de cancelar uma reserva já cancelada retornam `400 Bad Request` com a mensagem: `"Esta reserva já foi cancelada."`
- **Onde é validada:** [ReservaService.cs — linha 103](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Services/ReservaService.cs#L103-L104)

---

### RF010 — Limite de Capacidade da Turma

A capacidade máxima de alunos por turma deve ser um valor entre **1 e 50**. Valores fora desse intervalo são rejeitados com `400 Bad Request` tanto na criação quanto na atualização da turma.

- **Validação:** Atributo `[Range(1, 50)]` nos DTOs `TurmaCreateDTO` e `TurmaUpdateDTO`, e também no Model `Turma`.
- **Mensagem retornada:** `"A capacidade deve ser entre 1 e 50 alunos."`
- **Onde é validada:** [TurmaCreateDTO.cs — linha 30](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/DTOs/Turma/TurmaCreateDTO.cs#L29-L31) e [Turma.cs — linha 47](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Models/Turma.cs#L47-L48)

---

### RF011 — Identidade Extraída do Token (Segurança por Design)

O sistema **nunca aceita IDs de usuário via body da requisição** para operações sensíveis. O `AlunoId` (em reservas) e o `ProfessorId` (em turmas) são sempre extraídos do claim `NameIdentifier` do token JWT autenticado.

- **Motivação:** Impedir que um aluno crie reservas em nome de outro, ou que um professor associe turmas a outro professor.
- **Implementação:** O `ReservaCreateDTO` contém apenas o campo `TurmaId` — sem campo `AlunoId`.
- **Onde é implementada:** [ReservasController.cs — linha 109](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Controllers/ReservasController.cs#L109-L113) e [TurmasController.cs — linha 112](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Controllers/TurmasController.cs#L112-L116)

---

### RF012 — Integridade Referencial e Exclusão

O banco de dados aplica regras de integridade referencial para prevenir exclusão acidental de dados relacionados:

| Relacionamento | Comportamento ao Deletar | Justificativa |
|----------------|--------------------------|---------------|
| `Professor → Turma` | **Restrict** — impede deletar professor que possui turmas | Protege dados das turmas e reservas vinculadas |
| `Aluno → Reserva` | **Restrict** — impede deletar aluno que possui reservas | Preserva o histórico de agendamentos |
| `Turma → Reserva` | **Cascade** — ao deletar turma, remove suas reservas | Reservas sem turma não fazem sentido no domínio |

- **Onde é configurada:** [AppDbContext.cs — linhas 36-58](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Data/AppDbContext.cs#L36-L58)

---

### RF013 — Tratamento Padronizado de Erros

Todas as exceções lançadas pela camada de Service são interceptadas pelo middleware global e convertidas em respostas HTTP padronizadas, **sem vazamento de stack traces**:

| Tipo de Exceção | Status HTTP | Uso no Sistema |
|-----------------|-------------|----------------|
| `InvalidOperationException` | `400 Bad Request` | Violações de regras de negócio (RF001, RF002, RF008, RF009) |
| `ArgumentException` | `400 Bad Request` | Validação de parâmetros inválidos (RF007) |
| `UnauthorizedAccessException` | `401 Unauthorized` | Credenciais inválidas ou ação não permitida (RF004) |
| `KeyNotFoundException` | `404 Not Found` | Recurso não encontrado (turma inexistente ao reservar) |
| Qualquer outra exceção | `500 Internal Server Error` | Erro genérico, mensagem sanitizada: `"Ocorreu um erro interno no servidor."` |

- **Formato da resposta:** JSON com campos `status`, `message` e `timestamp` (UTC).
- **Onde é implementado:** [GlobalExceptionMiddleware.cs](file:///home/vinicius-dos-santos/Documentos/Ultima-Sprint-Ford-Enter-API/NatacaoAPI/Middleware/GlobalExceptionMiddleware.cs#L44-L73)

---

## 3. Funcionalidades Principais

### 🔐 Autenticação e Segurança
- **Registro de usuários** com definição de perfil (Aluno ou Professor) e validação de e-mail único.
- **Login com token JWT** contendo claims de identidade (`Id`, `Email`, `Nome`, `Role`), com expiração de 24 horas.
- **Hash de senhas com BCrypt** — senhas nunca são armazenadas em texto puro.
- **Validação de token** com verificação de Issuer, Audience, Lifetime e assinatura HMAC-SHA256.
- **Tratamento global de exceções** via middleware, sem vazamento de stack traces em produção.

### 📚 Gestão de Turmas
- **CRUD completo** de turmas com campos: nome, descrição, modalidade, data/hora de início e fim, capacidade máxima.
- **Cálculo dinâmico de vagas disponíveis** (`VagasDisponiveis = CapacidadeMaxima - ReservasAtivas`), retornado em tempo real em todas as consultas.
- **Atribuição automática do professor** responsável via token JWT, sem necessidade de informar manualmente.

### 📅 Agendamento de Reservas
- **Criação de reservas** com validação automática de capacidade (RF001) e conflito de horário (RF002).
- **Proteção contra reserva duplicada** na mesma turma pelo mesmo aluno.
- **Cancelamento por soft delete** — a reserva não é apagada do banco, apenas muda o status para `Cancelada`, mantendo histórico.
- **Segregação de dados** — alunos visualizam apenas suas reservas; professores visualizam todas.

### 🖥️ Frontend Integrado
- **Interface web responsiva** servida como arquivos estáticos via `wwwroot`.
- **Tema dark** com Bootstrap 5.3.
- **Interação com a API** via JavaScript (ES6+) com chamadas `fetch` autenticadas por JWT.

### 📖 Documentação Interativa
- **Swagger UI** disponível em `/swagger` (ambiente de desenvolvimento).
- **Suporte a autenticação** via botão "Authorize" com token Bearer diretamente na UI do Swagger.

### ✅ Testes Automatizados
- **Testes unitários com xUnit e Moq** cobrindo as regras de negócio críticas:
  - `ReservaServiceTests` — valida RF001 (capacidade), RF002 (conflito de horário) e RF004 (cancelamento por não-dono).
  - `TurmaServiceTests` — valida CRUD e cálculo de vagas disponíveis.

### 🐳 Containerização
- **Dockerfile multi-stage** para build otimizado e deploy via container.
- Imagem final baseada em `aspnet:8.0` (runtime only), sem SDK em produção.

### 🩺 Health Check
- **Endpoint `/health`** para monitoramento de disponibilidade da aplicação.
