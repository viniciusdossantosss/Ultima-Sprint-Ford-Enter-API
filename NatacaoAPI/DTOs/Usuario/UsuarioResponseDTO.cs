namespace NatacaoAPI.DTOs.Usuario
{
    public class UsuarioResponseDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataNascimento { get; set; }
        public string? NivelPedagogico { get; set; }
        public string? ModalidadeSugerida { get; set; }
        public string? Telefone { get; set; }
        public string? NomeResponsavel { get; set; }
        public string? TelefoneResponsavel { get; set; }
        public bool DocumentacaoSaudeEntregue { get; set; }
        public string? ProblemasSaude { get; set; }
    }
}
