using System.ComponentModel.DataAnnotations;

namespace NatacaoAPI.DTOs.Usuario
{
    /// <summary>
    /// DTO para criação de usuário pelo Admin.
    /// Inclui validação de complexidade de senha.
    /// </summary>
    public class UsuarioCreateDTO
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#]).{8,}$",
            ErrorMessage = "A senha deve conter maiúscula, minúscula, número e caractere especial.")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "O perfil é obrigatório.")]
        public string Role { get; set; } = "Aluno";

        // Campos opcionais/específicos para Aluno
        public DateTime? DataNascimento { get; set; }
        public string? Telefone { get; set; }
        public string? NomeResponsavel { get; set; }
        public string? TelefoneResponsavel { get; set; }
        public bool DocumentacaoSaudeEntregue { get; set; }
        public string? ProblemasSaude { get; set; }
    }
}
