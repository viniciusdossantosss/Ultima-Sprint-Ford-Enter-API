using NatacaoAPI.DTOs.Reserva;

namespace NatacaoAPI.Services.Interfaces
{
    public interface IReservaService
    {
        Task<IEnumerable<ReservaResponseDTO>> GetAllAsync();
        Task<IEnumerable<ReservaResponseDTO>> GetByAlunoIdAsync(int alunoId);
        Task<ReservaResponseDTO?> GetByIdAsync(int id);

        /// <summary>
        /// Cria uma reserva aplicando RF001 (capacidade) e RF002 (conflito de horário).
        /// Lança exceção com mensagem descritiva se alguma regra for violada.
        /// </summary>
        Task<ReservaResponseDTO> CreateAsync(ReservaCreateDTO dto, int alunoId);

        /// <summary>
        /// Cancela a reserva (soft delete via Status = Cancelada).
        /// Valida que o aluno é o dono da reserva.
        /// </summary>
        Task<bool> CancelAsync(int id, int alunoId);
    }
}
