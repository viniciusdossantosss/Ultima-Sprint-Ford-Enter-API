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
<!DOCTYPE html>
<html lang='pt-BR'>
<head><meta charset='UTF-8'></head>
<body style='margin:0;padding:0;background-color:#0a0e1a;'>
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#0a0e1a;'>
  <tr><td align='center' style='padding:40px 20px;'>
    <table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0' style='max-width:600px;width:100%;background-color:#0a0e1a;border-radius:16px;'>
      <tr><td style='text-align:center;padding:0 40px 30px;font-family:Inter,Arial,sans-serif;'>
        <h1 style='color:#06b6d4;margin:0;font-size:28px;'>🏊 AquaSchedule</h1>
        <p style='color:#94a3b8;margin-top:8px;font-size:14px;'>Sistema de Aulas de Natação</p>
      </td></tr>
      <tr><td style='padding:0 40px;font-family:Inter,Arial,sans-serif;color:#f1f5f9;'>
        <h2 style='color:#f1f5f9;margin:0 0 12px;'>Olá, {nome}!</h2>
        <p style='margin:0 0 20px;font-size:15px;line-height:1.6;'>Sua conta foi criada com sucesso. Aqui estão suas credenciais de acesso:</p>
      </td></tr>
      <tr><td style='padding:0 40px;font-family:Inter,Arial,sans-serif;'>
        <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#0d1a2a;border:1px solid #164e63;border-radius:12px;'>
          <tr><td style='padding:20px;color:#f1f5f9;font-size:15px;'>
            <p style='margin:0 0 8px;'><strong>📧 E-mail:</strong> {toEmail}</p>
            <p style='margin:0;'><strong>🔑 Senha temporária:</strong> {senhaTemporaria}</p>
          </td></tr>
        </table>
      </td></tr>
      <tr><td style='padding:20px 40px 0;font-family:Inter,Arial,sans-serif;'>
        <p style='color:#f59e0b;margin:0;font-size:14px;'>⚠️ Recomendamos alterar sua senha no primeiro acesso.</p>
      </td></tr>
      <tr><td style='padding:30px 40px 0;'>
        <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'><tr><td style='border-top:1px solid #1e293b;'></td></tr></table>
      </td></tr>
      <tr><td style='padding:20px 40px 40px;font-family:Inter,Arial,sans-serif;text-align:center;'>
        <p style='color:#64748b;font-size:13px;margin:0;line-height:1.5;'>
          Este email foi enviado automaticamente pelo AquaSchedule.<br>
          Se você não solicitou esta conta, ignore este email.
        </p>
      </td></tr>
    </table>
  </td></tr>
</table>
</body>
</html>";

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
<!DOCTYPE html>
<html lang='pt-BR'>
<head><meta charset='UTF-8'></head>
<body style='margin:0;padding:0;background-color:#0a0e1a;'>
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#0a0e1a;'>
  <tr><td align='center' style='padding:40px 20px;'>
    <table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0' style='max-width:600px;width:100%;background-color:#0a0e1a;border-radius:16px;'>
      <tr><td style='text-align:center;padding:0 40px 30px;font-family:Inter,Arial,sans-serif;'>
        <h1 style='color:#06b6d4;margin:0;font-size:28px;'>🏊 AquaSchedule</h1>
      </td></tr>
      <tr><td style='padding:0 40px;font-family:Inter,Arial,sans-serif;color:#f1f5f9;'>
        <h2 style='color:#f1f5f9;margin:0 0 12px;'>Olá, {nome}!</h2>
        <p style='margin:0 0 20px;font-size:15px;line-height:1.6;'>Recebemos uma solicitação para redefinir sua senha.</p>
      </td></tr>
      <tr><td style='padding:0 40px 30px;text-align:center;font-family:Inter,Arial,sans-serif;'>
        <table role='presentation' cellpadding='0' cellspacing='0' border='0' style='margin:0 auto;'>
          <tr><td style='background-color:#0891b2;border-radius:8px;'>
            <a href='{resetUrl}' style='display:inline-block;padding:14px 32px;color:#ffffff;font-family:Inter,Arial,sans-serif;font-size:16px;font-weight:600;text-decoration:none;border-radius:8px;'>
              Redefinir Minha Senha
            </a>
          </td></tr>
        </table>
      </td></tr>
      <tr><td style='padding:0 40px;font-family:Inter,Arial,sans-serif;'>
        <p style='color:#94a3b8;font-size:14px;margin:0 0 8px;line-height:1.5;'>Este link expira em <strong style='color:#f1f5f9;'>30 minutos</strong>.</p>
        <p style='color:#94a3b8;font-size:14px;margin:0 0 8px;line-height:1.5;'>Se o botão não funcionar, copie e cole este link no navegador:</p>
        <p style='color:#06b6d4;word-break:break-all;font-size:13px;margin:0 0 20px;'><a href='{resetUrl}' style='color:#06b6d4;'>{resetUrl}</a></p>
      </td></tr>
      <tr><td style='padding:0 40px;'>
        <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'><tr><td style='border-top:1px solid #1e293b;'></td></tr></table>
      </td></tr>
      <tr><td style='padding:20px 40px 40px;font-family:Inter,Arial,sans-serif;text-align:center;'>
        <p style='color:#64748b;font-size:13px;margin:0;line-height:1.5;'>
          Se você não solicitou esta redefinição, ignore este email.
        </p>
      </td></tr>
    </table>
  </td></tr>
</table>
</body>
</html>";

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
