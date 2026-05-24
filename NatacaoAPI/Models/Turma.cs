using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NatacaoAPI.Models
{
    /// <summary>
    /// Entidade de domínio que representa uma turma de natação.
    /// Agora suporta datas específicas, permitindo visualização em formato de calendário.
    /// Mantém as regras de negócio de capacidade máxima (RF001).
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

        /// <summary>
        /// Data e Hora de início exatas da aula (ex: 2024-10-25 08:00).
        /// </summary>
        [Required(ErrorMessage = "A data e hora de início são obrigatórias.")]
        public DateTime DataHoraInicio { get; set; }

        /// <summary>
        /// Data e Hora de término exatas da aula (ex: 2024-10-25 09:00).
        /// </summary>
        [Required(ErrorMessage = "A data e hora de término são obrigatórias.")]
        public DateTime DataHoraFim { get; set; }

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