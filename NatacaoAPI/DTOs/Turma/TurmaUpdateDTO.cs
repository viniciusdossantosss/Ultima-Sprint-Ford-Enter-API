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

        [Required]
        [Range(1, 6)]
        public int DiaSemana { get; set; }

        [Required(ErrorMessage = "O horário de início é obrigatório.")]
        public string HorarioInicio { get; set; } = string.Empty;

        [Required(ErrorMessage = "O horário de fim é obrigatório.")]
        public string HorarioFim { get; set; } = string.Empty;

        [Required]
        [Range(1, 50)]
        public int CapacidadeMaxima { get; set; }
    }
}
