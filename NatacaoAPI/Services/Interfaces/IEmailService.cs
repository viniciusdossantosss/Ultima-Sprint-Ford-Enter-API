namespace NatacaoAPI.Services.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Envia email de boas-vindas com credenciais temporárias.
        /// </summary>
        Task SendWelcomeEmailAsync(string toEmail, string nome, string senhaTemporaria);

        /// <summary>
        /// Envia email com link de recuperação de senha.
        /// </summary>
        Task SendPasswordResetEmailAsync(string toEmail, string nome, string resetToken, string? baseUrl = null);
    }
}
