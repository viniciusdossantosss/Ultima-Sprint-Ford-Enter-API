using System;
using System.ComponentModel.DataAnnotations;

namespace NatacaoAPI.DTOs.Usuario
{
    /// <summary>
    /// DTO para atualização de cadastro de usuário pelo Admin.
    /// Não contém o campo Senha por motivos de segurança e boas práticas.
    /// </summary>
    public class UsuarioUpdateDTO
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        // Campos específicos para Aluno
        public DateTime? DataNascimento { get; set; }
        public string? Telefone { get; set; }
        public string? NomeResponsavel { get; set; }
        public string? TelefoneResponsavel { get; set; }
        public bool DocumentacaoSaudeEntregue { get; set; }
        public string? ProblemasSaude { get; set; }
        public string? NivelPedagogico { get; set; }

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
