using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NatacaoAPI.Models
{
    /// <summary>
    /// Status possíveis de uma reserva.
    /// Decisão: usar enum ao invés de bool "Cancelada" para extensibilidade futura
    /// (ex: Pendente, Confirmada, ListaEspera).
    /// </summary>
    public enum StatusReserva
    {
        Ativa = 0,
        Cancelada = 1
    }

    /// <summary>
    /// Entidade que associa um Aluno a uma Turma.
    /// As regras RF001 (capacidade) e RF002 (conflito de horário)
    /// são validadas na camada de Service antes da persistência.
    /// </summary>
    public class Reserva
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AlunoId { get; set; }

        [ForeignKey("AlunoId")]
        public Usuario Aluno { get; set; } = null!;

        [Required]
        public int TurmaId { get; set; }

        [ForeignKey("TurmaId")]
        public Turma Turma { get; set; } = null!;

        /// <summary>
        /// Data/hora em que a reserva foi criada.
        /// </summary>
        public DateTime DataReserva { get; set; } = DateTime.UtcNow;

        [Required]
        public StatusReserva Status { get; set; } = StatusReserva.Ativa;
    }
}
