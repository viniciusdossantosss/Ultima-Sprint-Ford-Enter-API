using NatacaoAPI.Models;

namespace NatacaoAPI.Repositories.Interfaces
{
    public interface ITurmaRepository
    {
        Task<IEnumerable<Turma>> GetAllAsync();
        Task<Turma?> GetByIdAsync(int id);
        Task<Turma> CreateAsync(Turma turma);
        Task<Turma> UpdateAsync(Turma turma);
        Task DeleteAsync(Turma turma);

        /// <summary>
        /// Conta as reservas ativas de uma turma. Usado para verificar RF001 (capacidade).
        /// </summary>
        Task<int> GetActiveReservasCountAsync(int turmaId);
    }
}
