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
    /// - Atribuição automática do ProfessorId (extraído do token JWT)
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
            return dto;
        }

        public async Task<TurmaResponseDTO> CreateAsync(TurmaCreateDTO createDto, int professorId)
        {
            var turma = _mapper.Map<Turma>(createDto);
            turma.ProfessorId = professorId;

            var created = await _turmaRepository.CreateAsync(turma);

            // Recarregar com Include do Professor para o mapeamento
            var fullTurma = await _turmaRepository.GetByIdAsync(created.Id);
            var dto = _mapper.Map<TurmaResponseDTO>(fullTurma!);
            dto.VagasDisponiveis = fullTurma!.CapacidadeMaxima;
            return dto;
        }

        public async Task<TurmaResponseDTO?> UpdateAsync(int id, TurmaUpdateDTO updateDto, int professorId)
        {
            var turma = await _turmaRepository.GetByIdAsync(id);
            if (turma == null) return null;

            // Validar ownership — Professor só pode editar suas próprias turmas (Admin pode tudo)
            if (turma.ProfessorId != professorId)
            {
                var performingUser = await _usuarioRepository.GetByIdAsync(professorId);
                if (performingUser == null || performingUser.Role != UsuarioRole.Admin)
                    throw new UnauthorizedAccessException("Você não tem permissão para editar esta turma.");
            }

            // Atualizar campos mantendo Id e ProfessorId
            turma.Nome = updateDto.Nome;
            turma.Descricao = updateDto.Descricao;
            turma.Modalidade = updateDto.Modalidade;
            turma.DataHoraInicio = updateDto.DataHoraInicio;
            turma.DataHoraFim = updateDto.DataHoraFim;
            turma.CapacidadeMaxima = updateDto.CapacidadeMaxima;

            await _turmaRepository.UpdateAsync(turma);

            var dto = _mapper.Map<TurmaResponseDTO>(turma);
            var reservasAtivas = await _turmaRepository.GetActiveReservasCountAsync(id);
            dto.VagasDisponiveis = turma.CapacidadeMaxima - reservasAtivas;
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
    }
}