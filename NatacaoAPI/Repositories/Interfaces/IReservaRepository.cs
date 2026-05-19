using NatacaoAPI.Models;

namespace NatacaoAPI.Repositories.Interfaces
{
    public interface IReservaRepository
    {
        Task<IEnumerable<Reserva>> GetAllAsync();
        Task<IEnumerable<Reserva>> GetByAlunoIdAsync(int alunoId);
        Task<Reserva?> GetByIdAsync(int id);
        Task<Reserva> CreateAsync(Reserva reserva);
        Task<Reserva> UpdateAsync(Reserva reserva);

        /// <summary>
        /// Verifica se o aluno já possui uma reserva ativa em uma turma
        /// que conflita com o mesmo DiaSemana e intervalo de horário. (RF002)
        /// </summary>
        Task<bool> AlunoHasConflictAsync(int alunoId, DiaSemana diaSemana,
            TimeSpan horarioInicio, TimeSpan horarioFim);

        /// <summary>
        /// Verifica se o aluno já possui reserva ativa nesta mesma turma.
        /// </summary>
        Task<bool> AlunoJaReservouTurmaAsync(int alunoId, int turmaId);
    }
}
