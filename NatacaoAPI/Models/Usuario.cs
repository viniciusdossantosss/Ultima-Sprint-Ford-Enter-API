using System.ComponentModel.DataAnnotations;

namespace NatacaoAPI.Models
{
    /// <summary>
    /// Enum que define os perfis de acesso do sistema.
    /// Decisão: usar enum com conversão para string no banco para legibilidade nas queries SQL.
    /// </summary>
    public enum UsuarioRole
    {
        Aluno = 0,
        Professor = 1,
        Admin = 2
    }

    /// <summary>
    /// Entidade rica de domínio representando um usuário do sistema.
    /// Um usuário pode ser Aluno (faz reservas), Professor (gerencia turmas) ou Admin (gerencia usuários).
    /// </summary>
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [MaxLength(150)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hash BCrypt da senha. Nunca armazenamos senhas em texto plano.
        /// </summary>
        [Required]
        public string SenhaHash { get; set; } = string.Empty;

        [Required]
        public UsuarioRole Role { get; set; } = UsuarioRole.Aluno;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // ─── Account Lockout ────────────────────────────────────────
        public int TentativasLoginFalhas { get; set; } = 0;
        public bool ContaBloqueada { get; set; } = false;
        public DateTime? BloqueioAte { get; set; }

        // ─── Password Reset ─────────────────────────────────────────
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        // ─── Cadastro de Aluno (Idade e Saúde) ─────────────────────
        public DateTime? DataNascimento { get; set; }

        [MaxLength(50)]
        public string? NivelPedagogico { get; set; }

        [MaxLength(50)]
        public string? ModalidadeSugerida { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [MaxLength(100)]
        public string? NomeResponsavel { get; set; }

        [MaxLength(20)]
        public string? TelefoneResponsavel { get; set; }

        public bool DocumentacaoSaudeEntregue { get; set; } = false;

        [MaxLength(500)]
        public string? ProblemasSaude { get; set; }

        // ─── Navegação ──────────────────────────────────────────────
        // Turmas que este usuário leciona (quando Role == Professor)
        public ICollection<Turma> TurmasLecionadas { get; set; } = new List<Turma>();

        // Reservas que este usuário fez (quando Role == Aluno)
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}
