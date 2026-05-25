using System.ComponentModel.DataAnnotations;

namespace NatacaoAPI.DTOs.Turma
{
    public class TurmaUpdateDTO
    {
        [Required(ErrorMessage = "O nome da turma é obrigatório.")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "A modalidade é obrigatória.")]
        public string Modalidade { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data e hora de início são obrigatórias.")]
        public DateTime DataHoraInicio { get; set; }

        [Required(ErrorMessage = "A data e hora de término são obrigatórias.")]
        public DateTime DataHoraFim { get; set; }

        [Required]
        [Range(1, 50)]
        public int CapacidadeMaxima { get; set; }

        public int? ProfessorId { get; set; }
    }
}