using Microsoft.EntityFrameworkCore;
using NatacaoAPI.Data;
using NatacaoAPI.Models;
using NatacaoAPI.Repositories.Interfaces;

namespace NatacaoAPI.Repositories
{
    public class TurmaRepository : ITurmaRepository
    {
        private readonly AppDbContext _context;

        public TurmaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Turma>> GetAllAsync()
        {
            return await _context.Turmas
                .Include(t => t.Professor)
                .Include(t => t.Reservas)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Turma?> GetByIdAsync(int id)
        {
            return await _context.Turmas
                .Include(t => t.Professor)
                .Include(t => t.Reservas)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Turma> CreateAsync(Turma turma)
        {
            _context.Turmas.Add(turma);
            await _context.SaveChangesAsync();
            return turma;
        }

        public async Task<Turma> UpdateAsync(Turma turma)
        {
            _context.Turmas.Update(turma);
            await _context.SaveChangesAsync();
            return turma;
        }

        public async Task DeleteAsync(Turma turma)
        {
            _context.Turmas.Remove(turma);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetActiveReservasCountAsync(int turmaId)
        {
            return await _context.Reservas
                .CountAsync(r => r.TurmaId == turmaId && r.Status == StatusReserva.Ativa);
        }
    }
}
