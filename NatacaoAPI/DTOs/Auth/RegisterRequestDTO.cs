using System.ComponentModel.DataAnnotations;

namespace NatacaoAPI.DTOs.Auth
{
    public class RegisterRequestDTO
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        /// <summary>
        /// Aceita "Aluno" ou "Professor". Default: "Aluno".
        /// </summary>
        public string Role { get; set; } = "Aluno";
    }
}
