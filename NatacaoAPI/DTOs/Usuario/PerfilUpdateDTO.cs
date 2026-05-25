using System;
using System.ComponentModel.DataAnnotations;

namespace NatacaoAPI.DTOs.Usuario
{
    /// <summary>
    /// DTO para atualização do próprio perfil pelo Aluno, Professor ou Admin.
    /// Permite alterar dados cadastrais e trocar a senha com verificação de segurança.
    /// </summary>
    public class PerfilUpdateDTO
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
        public string? ProblemasSaude { get; set; }

        // Campos específicos para Professor
        public string? Cref { get; set; }
        public bool? CrefAtivo { get; set; }
        public bool? AptoBebes { get; set; }
        public bool? AptoInfantil { get; set; }
        public bool? AptoAdulto { get; set; }
        public bool? AptoAltaPerformance { get; set; }
        public bool? AptoHidroginastica { get; set; }
        public bool? AptoPcd { get; set; }
        public DateTime? ValidadeSalvamentoAquatico { get; set; }
        public DateTime? ValidadePrimeirosSocorros { get; set; }

        // Campos de alteração de senha
        public string? SenhaAtual { get; set; }

        [MinLength(8, ErrorMessage = "A nova senha deve ter pelo menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#]).{8,}$",
            ErrorMessage = "A nova senha deve conter maiúscula, minúscula, número e caractere especial.")]
        public string? NovaSenha { get; set; }
    }
}
