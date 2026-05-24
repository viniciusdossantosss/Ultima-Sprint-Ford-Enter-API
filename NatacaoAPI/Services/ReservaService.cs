using Mapster;
using MapsterMapper;
using NatacaoAPI.DTOs.Reserva;
using NatacaoAPI.Models;
using NatacaoAPI.Repositories.Interfaces;
using NatacaoAPI.Services.Interfaces;

namespace NatacaoAPI.Services
{
    /// <summary>
    /// Serviço de Reservas — contém as regras de negócio mais críticas do sistema:
    /// 
    /// RF001 - Capacidade máxima: impede nova reserva se a turma está lotada.
    /// RF002 - Conflito de horário: impede reserva se o aluno já tem aula no mesmo horário.
    /// 
    /// Decisão: lançar InvalidOperationException com mensagem descritiva para que o
    /// Controller retorne 400 Bad Request com a explicação da violação.
    /// </summary>
    public class ReservaService : IReservaService
    {
        private readonly IReservaRepository _reservaRepository;
        private readonly ITurmaRepository _turmaRepository;
        private readonly IMapper _mapper;

        public ReservaService(
            IReservaRepository reservaRepository,
            ITurmaRepository turmaRepository,
            IMapper mapper)
        {
            _reservaRepository = reservaRepository;
            _turmaRepository = turmaRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ReservaResponseDTO>> GetAllAsync()
        {
            var reservas = await _reservaRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ReservaResponseDTO>>(reservas);
        }

        public async Task<IEnumerable<ReservaResponseDTO>> GetByAlunoIdAsync(int alunoId)
        {
            var reservas = await _reservaRepository.GetByAlunoIdAsync(alunoId);
            return _mapper.Map<IEnumerable<ReservaResponseDTO>>(reservas);
        }

        public async Task<ReservaResponseDTO?> GetByIdAsync(int id)
        {
            var reserva = await _reservaRepository.GetByIdAsync(id);
            if (reserva == null) return null;
            return _mapper.Map<ReservaResponseDTO>(reserva);
        }

        public async Task<ReservaResponseDTO> CreateAsync(ReservaCreateDTO dto, int alunoId)
        {
            // Buscar a turma para validações
            var turma = await _turmaRepository.GetByIdAsync(dto.TurmaId);
            if (turma == null)
                throw new KeyNotFoundException("Turma não encontrada.");

            // ─── RF001: Verificar capacidade máxima ──────────────────
            var reservasAtivas = await _turmaRepository.GetActiveReservasCountAsync(dto.TurmaId);
            if (reservasAtivas >= turma.CapacidadeMaxima)
                throw new InvalidOperationException(
                    $"RF001: A turma '{turma.Nome}' já atingiu a capacidade máxima de {turma.CapacidadeMaxima} alunos.");

            // Verificar se o aluno já está nesta turma
            if (await _reservaRepository.AlunoJaReservouTurmaAsync(alunoId, dto.TurmaId))
                throw new InvalidOperationException(
                    "Você já possui uma reserva ativa nesta turma.");

            // ─── RF002: Verificar conflito de horário ────────────────
            if (await _reservaRepository.AlunoHasConflictAsync(
                    alunoId, turma.DataHoraInicio, turma.DataHoraFim))
                throw new InvalidOperationException(
                    $"RF002: Você já possui uma aula agendada que conflita com este horário ({turma.DataHoraInicio:g} - {turma.DataHoraFim:t}).");

            // Criar a reserva
            var reserva = new Reserva
            {
                AlunoId = alunoId,
                TurmaId = dto.TurmaId,
                DataReserva = DateTime.UtcNow,
                Status = StatusReserva.Ativa
            };

            var created = await _reservaRepository.CreateAsync(reserva);

            // Recarregar com Includes para o mapeamento
            var fullReserva = await _reservaRepository.GetByIdAsync(created.Id);
            return _mapper.Map<ReservaResponseDTO>(fullReserva!);
        }

        public async Task<bool> CancelAsync(int id, int alunoId)
        {
            var reserva = await _reservaRepository.GetByIdAsync(id);
            if (reserva == null) return false;

            // RF004: Aluno só pode cancelar suas próprias reservas
            if (reserva.AlunoId != alunoId)
                throw new UnauthorizedAccessException(
                    "Você não tem permissão para cancelar esta reserva.");

            if (reserva.Status == StatusReserva.Cancelada)
                throw new InvalidOperationException("Esta reserva já foi cancelada.");

            reserva.Status = StatusReserva.Cancelada;
            await _reservaRepository.UpdateAsync(reserva);
            return true;
        }
    }
}