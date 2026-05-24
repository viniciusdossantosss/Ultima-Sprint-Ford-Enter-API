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
        /// Data e Hora de início exatas da aula (ex: 2024-10-25 08:00).
        /// </summary>
        [Required(ErrorMessage = "A data e hora de início são obrigatórias.")]
        public DateTime DataHoraInicio { get; set; }

        /// <summary>
        /// Data e Hora de término exatas da aula (ex: 2024-10-25 09:00).
        /// </summary>
        [Required(ErrorMessage = "A data e hora de término são obrigatórias.")]
        public DateTime DataHoraFim { get; set; }

        [Required]
        [Range(1, 50, ErrorMessage = "A capacidade deve ser entre 1 e 50.")]
        public int CapacidadeMaxima { get; set; }
    }
}