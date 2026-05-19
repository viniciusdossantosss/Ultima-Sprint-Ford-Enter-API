using NatacaoAPI.DTOs.Turma;

namespace NatacaoAPI.Services.Interfaces
{
    public interface ITurmaService
    {
        Task<IEnumerable<TurmaResponseDTO>> GetAllAsync();
        Task<TurmaResponseDTO?> GetByIdAsync(int id);
        Task<TurmaResponseDTO> CreateAsync(TurmaCreateDTO dto, int professorId);
        Task<TurmaResponseDTO?> UpdateAsync(int id, TurmaUpdateDTO dto, int professorId);
        Task<bool> DeleteAsync(int id, int professorId);
    }
}
