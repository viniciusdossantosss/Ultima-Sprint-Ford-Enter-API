using MailKit.Net.Smtp;
using MimeKit;
using NatacaoAPI.Services.Interfaces;

namespace NatacaoAPI.Services
{
    /// <summary>
    /// Serviço de email via Gmail SMTP (MailKit).
    /// Configuração: appsettings.json → seção "Email"
    /// 
    /// Para usar com Gmail: gerar uma "Senha de App" em
    /// myaccount.google.com/apppasswords
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string nome, string senhaTemporaria)
        {
            var subject = "🏊 Bem-vindo ao AquaSchedule!";
            var body = $@"
            <div style='font-family: Inter, Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #0a0e1a; color: #f1f5f9; padding: 40px; border-radius: 16px;'>
                <div style='text-align: center; margin-bottom: 30px;'>
                    <h1 style='color: #06b6d4; margin: 0;'>🏊 AquaSchedule</h1>
                    <p style='color: #94a3b8; margin-top: 8px;'>Sistema de Aulas de Natação</p>
                </div>
                <h2 style='color: #f1f5f9;'>Olá, {nome}!</h2>
                <p>Sua conta foi criada com sucesso. Aqui estão suas credenciais de acesso:</p>
                <div style='background: rgba(6, 182, 212, 0.1); border: 1px solid rgba(6, 182, 212, 0.3); border-radius: 12px; padding: 20px; margin: 20px 0;'>
                    <p><strong>📧 E-mail:</strong> {toEmail}</p>
                    <p><strong>🔑 Senha temporária:</strong> {senhaTemporaria}</p>
                </div>
                <p style='color: #f59e0b;'>⚠️ Recomendamos alterar sua senha no primeiro acesso.</p>
                <hr style='border-color: rgba(148, 163, 184, 0.1); margin: 30px 0;'>
                <p style='color: #64748b; font-size: 0.85rem; text-align: center;'>
                    Este email foi enviado automaticamente pelo AquaSchedule.<br>
                    Se você não solicitou esta conta, ignore este email.
                </p>
            </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string nome, string resetToken)
        {
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5000";
            var resetUrl = $"{frontendUrl}/?resetToken={resetToken}";

            var subject = "🔑 Redefinição de Senha — AquaSchedule";
            var body = $@"
            <div style='font-family: Inter, Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #0a0e1a; color: #f1f5f9; padding: 40px; border-radius: 16px;'>
                <div style='text-align: center; margin-bottom: 30px;'>
                    <h1 style='color: #06b6d4; margin: 0;'>🏊 AquaSchedule</h1>
                </div>
                <h2 style='color: #f1f5f9;'>Olá, {nome}!</h2>
                <p>Recebemos uma solicitação para redefinir sua senha.</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{resetUrl}' style='background: linear-gradient(135deg, #0891b2, #06b6d4); color: #fff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; display: inline-block;'>
                        Redefinir Minha Senha
                    </a>
                </div>
                <p style='color: #94a3b8; font-size: 0.9rem;'>Este link expira em <strong>30 minutos</strong>.</p>
                <p style='color: #94a3b8; font-size: 0.9rem;'>Se o botão não funcionar, copie e cole este link no navegador:</p>
                <p style='color: #06b6d4; word-break: break-all; font-size: 0.85rem;'>{resetUrl}</p>
                <hr style='border-color: rgba(148, 163, 184, 0.1); margin: 30px 0;'>
                <p style='color: #64748b; font-size: 0.85rem; text-align: center;'>
                    Se você não solicitou esta redefinição, ignore este email.
                </p>
            </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var emailSettings = _configuration.GetSection("Email");
                var smtpHost = emailSettings["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
                var senderEmail = emailSettings["SenderEmail"] ?? "";
                var senderName = emailSettings["SenderName"] ?? "AquaSchedule";
                var password = emailSettings["Password"] ?? "";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email enviado com sucesso para {Email}", toEmail);
            }
            catch (Exception ex)
            {
                // Log mas não crash — email falhando não deve impedir o fluxo
                _logger.LogError(ex, "Falha ao enviar email para {Email}", toEmail);
            }
        }
    }
}
