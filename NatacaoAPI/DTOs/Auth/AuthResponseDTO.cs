namespace NatacaoAPI.DTOs.Auth
{
    /// <summary>
    /// DTO de resposta para login/registro.
    /// Contém o token JWT que o frontend armazena para requisições autenticadas.
    /// </summary>
    public class AuthResponseDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
