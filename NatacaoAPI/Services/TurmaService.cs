using System.Linq;
using Mapster;
using MapsterMapper;
using NatacaoAPI.DTOs.Turma;
using NatacaoAPI.Models;
using NatacaoAPI.Repositories.Interfaces;
using NatacaoAPI.Services.Interfaces;

namespace NatacaoAPI.Services
{
    /// <summary>
    /// Serviço de Turmas.
    /// O Controller nunca acessa o repositório diretamente — toda lógica passa por aqui.
    /// 
    /// Responsabilidades:
    /// - CRUD com mapeamento DTO ↔ Model via AutoMapper
    /// - Cálculo dinâmico de VagasDisponiveis
    /// - Atribuição do ProfessorId (automática para Professor, via DTO para Admin)
    /// - Validação de CREF ativo e matriz de aptidão do professor
    /// - Flag de inconformidade caso as certificações do professor estejam expiradas
    /// </summary>
    public class TurmaService : ITurmaService
    {
        private readonly ITurmaRepository _turmaRepository;
        private readonly IMapper _mapper;
        private readonly IUsuarioRepository _usuarioRepository;

        public TurmaService(ITurmaRepository turmaRepository, IMapper mapper, IUsuarioRepository usuarioRepository)
        {
            _turmaRepository = turmaRepository;
            _mapper = mapper;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<IEnumerable<TurmaResponseDTO>> GetAllAsync()
        {
            var turmas = await _turmaRepository.GetAllAsync();
            var dtos = new List<TurmaResponseDTO>();

            foreach (var turma in turmas)
            {
                var dto = _mapper.Map<TurmaResponseDTO>(turma);
                var reservasAtivas = await _turmaRepository.GetActiveReservasCountAsync(turma.Id);
                dto.VagasDisponiveis = turma.CapacidadeMaxima - reservasAtivas;
                
                // Popular alunos inscritos
                dto.AlunosInscritos = turma.Reservas
                    .Where(r => r.Status == StatusReserva.Ativa && r.Aluno != null)
                    .Select(r => new AlunoInscritoDTO
                    {
                        Id = r.AlunoId,
                        Nome = r.Aluno.Nome,
                        Email = r.Aluno.Email,
                        NivelPedagogico = r.Aluno.NivelPedagogico
                    })
                    .ToList();
                
                // Processar alertas de certificação do professor
                ProcessarCertificacoesProfessor(dto, turma.Professor);

                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<TurmaResponseDTO?> GetByIdAsync(int id)
        {
            var turma = await _turmaRepository.GetByIdAsync(id);
            if (turma == null) return null;

            var dto = _mapper.Map<TurmaResponseDTO>(turma);
            var reservasAtivas = await _turmaRepository.GetActiveReservasCountAsync(id);
            dto.VagasDisponiveis = turma.CapacidadeMaxima - reservasAtivas;
            
            // Popular alunos inscritos
            dto.AlunosInscritos = turma.Reservas
                .Where(r => r.Status == StatusReserva.Ativa && r.Aluno != null)
                .Select(r => new AlunoInscritoDTO
                {
                    Id = r.AlunoId,
                    Nome = r.Aluno.Nome,
                    Email = r.Aluno.Email,
                    NivelPedagogico = r.Aluno.NivelPedagogico
                })
                .ToList();
            
            // Processar alertas de certificação do professor
            ProcessarCertificacoesProfessor(dto, turma.Professor);

            return dto;
        }

        public async Task<TurmaResponseDTO> CreateAsync(TurmaCreateDTO createDto, int professorId)
        {
            int targetProfessorId;
            var creator = await _usuarioRepository.GetByIdAsync(professorId);
            
            if (creator != null && creator.Role == UsuarioRole.Admin)
            {
                if (!createDto.ProfessorId.HasValue)
                    throw new ArgumentException("O ProfessorId é obrigatório quando a turma é criada por um administrador.");
                targetProfessorId = createDto.ProfessorId.Value;
            }
            else
            {
                targetProfessorId = professorId;
            }

            // Validar aptidões e CREF do professor
            await ValidarProfessorParaTurmaAsync(targetProfessorId, createDto.Modalidade);

            var turma = _mapper.Map<Turma>(createDto);
            turma.ProfessorId = targetProfessorId;

            var created = await _turmaRepository.CreateAsync(turma);

            // Recarregar com Include do Professor para o mapeamento
            var fullTurma = await _turmaRepository.GetByIdAsync(created.Id);
            var dto = _mapper.Map<TurmaResponseDTO>(fullTurma!);
            dto.VagasDisponiveis = fullTurma!.CapacidadeMaxima;
            
            ProcessarCertificacoesProfessor(dto, fullTurma.Professor);

            return dto;
        }

        public async Task<TurmaResponseDTO?> UpdateAsync(int id, TurmaUpdateDTO updateDto, int professorId)
        {
            var turma = await _turmaRepository.GetByIdAsync(id);
            if (turma == null) return null;

            var performingUser = await _usuarioRepository.GetByIdAsync(professorId);
            
            // Validar ownership — Professor só pode editar suas próprias turmas (Admin pode tudo)
            if (turma.ProfessorId != professorId)
            {
                if (performingUser == null || performingUser.Role != UsuarioRole.Admin)
                    throw new UnauthorizedAccessException("Você não tem permissão para editar esta turma.");
            }

            int targetProfessorId = turma.ProfessorId;
            if (performingUser != null && performingUser.Role == UsuarioRole.Admin)
            {
                if (updateDto.ProfessorId.HasValue)
                {
                    targetProfessorId = updateDto.ProfessorId.Value;
                }
            }

            // Validar aptidões e CREF do professor
            await ValidarProfessorParaTurmaAsync(targetProfessorId, updateDto.Modalidade);

            // Atualizar campos
            turma.Nome = updateDto.Nome;
            turma.Descricao = updateDto.Descricao;
            turma.Modalidade = updateDto.Modalidade;
            turma.DataHoraInicio = updateDto.DataHoraInicio;
            turma.DataHoraFim = updateDto.DataHoraFim;
            turma.CapacidadeMaxima = updateDto.CapacidadeMaxima;
            turma.ProfessorId = targetProfessorId;

            await _turmaRepository.UpdateAsync(turma);

            // Recarregar com Include atualizado
            var fullTurma = await _turmaRepository.GetByIdAsync(id);
            var dto = _mapper.Map<TurmaResponseDTO>(fullTurma!);
            var reservasAtivas = await _turmaRepository.GetActiveReservasCountAsync(id);
            dto.VagasDisponiveis = fullTurma!.CapacidadeMaxima - reservasAtivas;
            
            ProcessarCertificacoesProfessor(dto, fullTurma.Professor);

            return dto;
        }

        public async Task<bool> DeleteAsync(int id, int professorId)
        {
            var turma = await _turmaRepository.GetByIdAsync(id);
            if (turma == null) return false;

            // Validar ownership — Professor só pode deletar suas próprias turmas (Admin pode tudo)
            if (turma.ProfessorId != professorId)
            {
                var performingUser = await _usuarioRepository.GetByIdAsync(professorId);
                if (performingUser == null || performingUser.Role != UsuarioRole.Admin)
                    throw new UnauthorizedAccessException("Você não tem permissão para deletar esta turma.");
            }

            await _turmaRepository.DeleteAsync(turma);
            return true;
        }

        private async Task ValidarProfessorParaTurmaAsync(int professorId, string modalidade)
        {
            var professor = await _usuarioRepository.GetByIdAsync(professorId);
            if (professor == null)
            {
                throw new ArgumentException("Professor não encontrado.");
            }

            if (professor.Role != UsuarioRole.Professor)
            {
                throw new ArgumentException("O usuário selecionado não é um professor.");
            }

            // Validar CREF
            if (string.IsNullOrWhiteSpace(professor.Cref) || !professor.CrefAtivo)
            {
                throw new ArgumentException($"O professor {professor.Nome} deve possuir um registro CREF preenchido e ativo.");
            }

            // Validar Aptidão
            if (string.IsNullOrWhiteSpace(modalidade)) return;

            var mod = modalidade.Trim().ToLowerInvariant();
            
            // Remover acentos para comparação mais segura
            mod = mod.Replace("á", "a")
                     .Replace("é", "e")
                     .Replace("í", "i")
                     .Replace("ó", "o")
                     .Replace("ú", "u")
                     .Replace("â", "a")
                     .Replace("ê", "e")
                     .Replace("ô", "o")
                     .Replace("ã", "a")
                     .Replace("õ", "o")
                     .Replace("ç", "c");

            bool apto = mod switch
            {
                "bebes" => professor.AptoBebes,
                "bebe" => professor.AptoBebes,
                "infantil" => professor.AptoInfantil,
                "adulto" => professor.AptoAdulto,
                "alta performance" => professor.AptoAltaPerformance,
                "hidroginastica" => professor.AptoHidroginastica,
                "pcd" => professor.AptoPcd,
                _ => true // Se for uma modalidade livre/outra que não exija aptidão específica
            };

            if (!apto)
            {
                throw new ArgumentException($"O professor {professor.Nome} não está apto para ministrar aulas da modalidade '{modalidade}'.");
            }
        }

        private void ProcessarCertificacoesProfessor(TurmaResponseDTO dto, Usuario professor)
        {
            if (professor == null) return;

            var hoje = DateTime.Today;
            bool salvamentoExpirado = professor.ValidadeSalvamentoAquatico.HasValue && professor.ValidadeSalvamentoAquatico.Value < hoje;
            bool primeirosSocorrosExpirado = professor.ValidadePrimeirosSocorros.HasValue && professor.ValidadePrimeirosSocorros.Value < hoje;

            if (salvamentoExpirado || primeirosSocorrosExpirado)
            {
                dto.ProfessorCertificacaoExpirada = true;
                var pendencias = new List<string>();
                if (salvamentoExpirado) pendencias.Add("Salvamento Aquático");
                if (primeirosSocorrosExpirado) pendencias.Add("Primeiros Socorros / RCP");
                dto.ProfessorInconformidadeMensagem = $"Certificações expiradas: {string.Join(", ", pendencias)}.";
            }
            else
            {
                dto.ProfessorCertificacaoExpirada = false;
                dto.ProfessorInconformidadeMensagem = null;
            }
        }
    }
}