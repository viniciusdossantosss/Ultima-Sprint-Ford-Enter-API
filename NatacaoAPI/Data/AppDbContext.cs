using Microsoft.EntityFrameworkCore;
using NatacaoAPI.Models;

namespace NatacaoAPI.Data
{
    /// <summary>
    /// Contexto do Entity Framework Core configurado para MySQL (Pomelo).
    /// 
    /// Decisões arquiteturais:
    /// - Enums armazenados como string no banco para legibilidade.
    /// - DeleteBehavior.Restrict em FK de Professor e Aluno para evitar
    ///   exclusão em cascata acidental de dados relacionados.
    /// - Índice único em Email para garantir unicidade a nível de banco.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Turma> Turmas { get; set; } = null!;
        public DbSet<Reserva> Reservas { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ─── Usuario ─────────────────────────────────────────────
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                // Armazena enum como string: "Aluno", "Professor"
                entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            });

            // ─── Turma ───────────────────────────────────────────────
            modelBuilder.Entity<Turma>(entity =>
            {
                entity.HasOne(t => t.Professor)
                      .WithMany(u => u.TurmasLecionadas)
                      .HasForeignKey(t => t.ProfessorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── Reserva ─────────────────────────────────────────────
            modelBuilder.Entity<Reserva>(entity =>
            {
                entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

                entity.HasOne(r => r.Aluno)
                      .WithMany(u => u.Reservas)
                      .HasForeignKey(r => r.AlunoId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Turma)
                      .WithMany(t => t.Reservas)
                      .HasForeignKey(r => r.TurmaId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}