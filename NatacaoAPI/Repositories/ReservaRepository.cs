using Microsoft.EntityFrameworkCore;
using NatacaoAPI.Data;
using NatacaoAPI.Models;
using NatacaoAPI.Repositories.Interfaces;

namespace NatacaoAPI.Repositories
{
    public class ReservaRepository : IReservaRepository
    {
        private readonly AppDbContext _context;

        public ReservaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Reserva>> GetAllAsync()
        {
            return await _context.Reservas
                .Include(r => r.Aluno)
                .Include(r => r.Turma)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Reserva>> GetByAlunoIdAsync(int alunoId)
        {
            return await _context.Reservas
                .Include(r => r.Aluno)
                .Include(r => r.Turma)
                .Where(r => r.AlunoId == alunoId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Reserva?> GetByIdAsync(int id)
        {
            return await _context.Reservas
                .Include(r => r.Aluno)
                .Include(r => r.Turma)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Reserva> CreateAsync(Reserva reserva)
        {
            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();
            return reserva;
        }

        public async Task<Reserva> UpdateAsync(Reserva reserva)
        {
            _context.Reservas.Update(reserva);
            await _context.SaveChangesAsync();
            return reserva;
        }

        /// <summary>
        /// RF002: Verifica conflito de horário.
        /// Um conflito ocorre se o aluno já tem uma reserva ativa cujo período
        /// se sobrepõe com o novo agendamento.
        /// Lógica de sobreposição: InicioA < FimB && InicioB < FimA
        /// </summary>
        public async Task<bool> AlunoHasConflictAsync(int alunoId, DateTime dataHoraInicio, DateTime dataHoraFim)
        {
            return await _context.Reservas
                .Include(r => r.Turma)
                .AnyAsync(r =>
                    r.AlunoId == alunoId &&
                    r.Status == StatusReserva.Ativa &&
                    r.Turma.DataHoraInicio < dataHoraFim &&
                    dataHoraInicio < r.Turma.DataHoraFim);
        }

        public async Task<bool> AlunoJaReservouTurmaAsync(int alunoId, int turmaId)
        {
            return await _context.Reservas
                .AnyAsync(r =>
                    r.AlunoId == alunoId &&
                    r.TurmaId == turmaId &&
                    r.Status == StatusReserva.Ativa);
        }
    }
}