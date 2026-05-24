using System.ComponentModel.DataAnnotations;

namespace NatacaoAPI.DTOs.Auth
{
    public class ResetPasswordDTO
    {
        [Required(ErrorMessage = "O token é obrigatório.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#]).{8,}$",
            ErrorMessage = "A senha deve conter maiúscula, minúscula, número e caractere especial.")]
        public string NovaSenha { get; set; } = string.Empty;
    }
}
