using System.ComponentModel.DataAnnotations;

namespace NatacaoAPI.DTOs.Turma
{
    public class TurmaCreateDTO
    {
        [Required(ErrorMessage = "O nome da turma é obrigatório.")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "A modalidade é obrigatória.")]
        public string Modalidade { get; set; } = string.Empty;

        /// <summary>
        /// Dia da semana (1=Segunda ... 6=Sábado).
        /// </summary>
        [Required]
        [Range(1, 6, ErrorMessage = "DiaSemana deve ser entre 1 (Segunda) e 6 (Sábado).")]
        public int DiaSemana { get; set; }

        /// <summary>
        /// Formato "HH:mm" (ex: "08:00").
        /// </summary>
        [Required(ErrorMessage = "O horário de início é obrigatório.")]
        public string HorarioInicio { get; set; } = string.Empty;

        [Required(ErrorMessage = "O horário de fim é obrigatório.")]
        public string HorarioFim { get; set; } = string.Empty;

        [Required]
        [Range(1, 50, ErrorMessage = "A capacidade deve ser entre 1 e 50.")]
        public int CapacidadeMaxima { get; set; }
    }
}
