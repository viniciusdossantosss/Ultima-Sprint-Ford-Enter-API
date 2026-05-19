using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NatacaoAPI.Models
{
    /// <summary>
    /// Enum para dias da semana das aulas.
    /// Valores iniciam em 1 para evitar ambiguidade com default(int) = 0.
    /// </summary>
    public enum DiaSemana
    {
        Segunda = 1,
        Terca = 2,
        Quarta = 3,
        Quinta = 4,
        Sexta = 5,
        Sabado = 6
    }

    /// <summary>
    /// Entidade de domínio que representa uma turma de natação.
    /// Cada turma possui um horário fixo semanal, um professor responsável,
    /// e uma capacidade máxima de alunos — regra de negócio central (RF001).
    /// </summary>
    public class Turma
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da turma é obrigatório.")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Descricao { get; set; } = string.Empty;

        /// <summary>
        /// Modalidade da aula: Infantil, Adulto, Hidroginástica, Livre, etc.
        /// </summary>
        [Required(ErrorMessage = "A modalidade é obrigatória.")]
        [MaxLength(50)]
        public string Modalidade { get; set; } = string.Empty;

        [Required]
        public DiaSemana DiaSemana { get; set; }

        [Required]
        public TimeSpan HorarioInicio { get; set; }

        [Required]
        public TimeSpan HorarioFim { get; set; }

        /// <summary>
        /// Capacidade máxima de alunos. Utilizada na RF001 para bloquear
        /// novas reservas quando o limite é atingido.
        /// </summary>
        [Required]
        [Range(1, 50, ErrorMessage = "A capacidade deve ser entre 1 e 50 alunos.")]
        public int CapacidadeMaxima { get; set; }

        // ─── Relacionamentos ─────────────────────────────────────────
        [Required]
        public int ProfessorId { get; set; }

        [ForeignKey("ProfessorId")]
        public Usuario Professor { get; set; } = null!;

        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}
