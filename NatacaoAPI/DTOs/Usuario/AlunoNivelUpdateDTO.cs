using System.ComponentModel.DataAnnotations;

namespace NatacaoAPI.DTOs.Usuario
{
    /// <summary>
    /// DTO para atualização do nível pedagógico de um Aluno por um Professor ou Admin.
    /// </summary>
    public class AlunoNivelUpdateDTO
    {
        [Required(ErrorMessage = "O nível pedagógico é obrigatório.")]
        [MaxLength(50)]
        public string NivelPedagogico { get; set; } = string.Empty;
    }
}
