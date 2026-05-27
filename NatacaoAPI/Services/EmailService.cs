using System.Net.Http;
using System.Net.Http.Json;
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
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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

        public async Task SendPasswordResetEmailAsync(string toEmail, string nome, string resetToken, string? baseUrl = null)
        {
            var frontendUrl = baseUrl;
            if (string.IsNullOrEmpty(frontendUrl))
            {
                frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5000";
            }
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
                var provider = emailSettings["Provider"] ?? "SMTP";
                var senderEmail = emailSettings["SenderEmail"] ?? "seu-email@gmail.com";
                var senderName = emailSettings["SenderName"] ?? "AquaSchedule";

                if (string.Equals(provider, "Log", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "============================================================\n" +
                        "SIMULAÇÃO DE ENVIO DE E-MAIL (PROVEDOR LOG):\n" +
                        "De: {SenderName} <{SenderEmail}>\n" +
                        "Para: {ToEmail}\n" +
                        "Assunto: {Subject}\n" +
                        "Conteúdo:\n{HtmlBody}\n" +
                        "============================================================",
                        senderName, senderEmail, toEmail, subject, htmlBody);
                    return;
                }

                if (string.Equals(provider, "Resend", StringComparison.OrdinalIgnoreCase))
                {
                    var apiKey = emailSettings["ApiKey"];
                    if (string.IsNullOrWhiteSpace(apiKey))
                        throw new InvalidOperationException("Resend ApiKey não configurada.");

                    using var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                    var payload = new
                    {
                        from = $"{senderName} <{senderEmail}>",
                        to = new[] { toEmail },
                        subject = subject,
                        html = htmlBody
                    };

                    var response = await client.PostAsJsonAsync("https://api.resend.com/emails", payload);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new HttpRequestException($"Erro na API do Resend ({response.StatusCode}): {errorContent}");
                    }

                    _logger.LogInformation("Email enviado com sucesso via Resend para {Email}", toEmail);
                    return;
                }

                if (string.Equals(provider, "Brevo", StringComparison.OrdinalIgnoreCase))
                {
                    var apiKey = emailSettings["ApiKey"];
                    if (string.IsNullOrWhiteSpace(apiKey))
                        throw new InvalidOperationException("Brevo ApiKey não configurada.");

                    using var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Add("api-key", apiKey);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    var payload = new
                    {
                        sender = new { name = senderName, email = senderEmail },
                        to = new[] { new { email = toEmail } },
                        subject = subject,
                        htmlContent = htmlBody
                    };

                    var response = await client.PostAsJsonAsync("https://api.brevo.com/v3/smtp/email", payload);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new HttpRequestException($"Erro na API do Brevo ({response.StatusCode}): {errorContent}");
                    }

                    _logger.LogInformation("Email enviado com sucesso via Brevo para {Email}", toEmail);
                    return;
                }

                // Default: SMTP
                var smtpHost = emailSettings["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
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

                using var smtpClient = new SmtpClient();
                smtpClient.Timeout = 5000; // 5 segundos
                await smtpClient.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await smtpClient.AuthenticateAsync(senderEmail, password);
                await smtpClient.SendAsync(message);
                await smtpClient.DisconnectAsync(true);

                _logger.LogInformation("Email enviado com sucesso via SMTP para {Email}", toEmail);
            }
            catch (Exception ex)
            {
                // Log mas não crash — email falhando não deve impedir o fluxo
                _logger.LogError(ex, "Falha ao enviar email para {Email}", toEmail);
            }
        }
    }
}
