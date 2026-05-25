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

        public string? Senha { get; set; }

        [Required(ErrorMessage = "O perfil é obrigatório.")]
        public string Role { get; set; } = "Aluno";

        // Campos opcionais/específicos para Aluno
        public DateTime? DataNascimento { get; set; }
        public string? Telefone { get; set; }
        public string? NomeResponsavel { get; set; }
        public string? TelefoneResponsavel { get; set; }
        public bool DocumentacaoSaudeEntregue { get; set; }
        public string? ProblemasSaude { get; set; }

        // Campos opcionais/específicos para Professor
        public string? Cref { get; set; }
        public bool CrefAtivo { get; set; }
        public bool AptoBebes { get; set; }
        public bool AptoInfantil { get; set; }
        public bool AptoAdulto { get; set; }
        public bool AptoAltaPerformance { get; set; }
        public bool AptoHidroginastica { get; set; }
        public bool AptoPcd { get; set; }
        public DateTime? ValidadeSalvamentoAquatico { get; set; }
        public DateTime? ValidadePrimeirosSocorros { get; set; }
    }
}
