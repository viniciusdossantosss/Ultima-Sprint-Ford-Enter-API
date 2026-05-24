using System.ComponentModel.DataAnnotations;

namespace NatacaoAPI.DTOs.Auth
{
    public class ForgotPasswordDTO
    {
        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; } = string.Empty;
    }
}
