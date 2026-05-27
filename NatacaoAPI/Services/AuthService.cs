using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NatacaoAPI.DTOs.Auth;
using NatacaoAPI.Models;
using NatacaoAPI.Repositories.Interfaces;
using NatacaoAPI.Services.Interfaces;

namespace NatacaoAPI.Services
{
    /// <summary>
    /// Serviço de autenticação com:
    /// - Login com verificação BCrypt + geração de JWT
    /// - Account lockout após 5 tentativas falhas (15min de bloqueio)
    /// - Recuperação de senha via email (token com expiração de 30min)
    /// 
    /// NOTA: RegisterAsync foi removido. Apenas o Admin pode criar usuários
    /// via UsuarioService/UsuariosController.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        private const int MaxLoginAttempts = 5;
        private const int LockoutMinutes = 15;

        public AuthService(
            IUsuarioRepository usuarioRepository,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            var usuario = await _usuarioRepository.GetByEmailAsync(request.Email);

            if (usuario == null)
                throw new UnauthorizedAccessException("E-mail ou senha inválidos.");

            // Verificar account lockout
            if (usuario.ContaBloqueada && usuario.BloqueioAte.HasValue)
            {
                if (usuario.BloqueioAte.Value > DateTime.UtcNow)
                {
                    var minutosRestantes = (int)(usuario.BloqueioAte.Value - DateTime.UtcNow).TotalMinutes + 1;
                    throw new InvalidOperationException(
                        $"Conta bloqueada por excesso de tentativas. Tente novamente em {minutosRestantes} minuto(s).");
                }
                // Lockout expirou — resetar
                usuario.ContaBloqueada = false;
                usuario.TentativasLoginFalhas = 0;
                usuario.BloqueioAte = null;
            }

            // Verificar senha
            if (!BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
            {
                // Incrementar tentativas falhas
                usuario.TentativasLoginFalhas++;

                if (usuario.TentativasLoginFalhas >= MaxLoginAttempts)
                {
                    usuario.ContaBloqueada = true;
                    usuario.BloqueioAte = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    _logger.LogWarning("Conta bloqueada por excesso de tentativas: {Email}", usuario.Email);
                }

                await _usuarioRepository.UpdateAsync(usuario);
                throw new UnauthorizedAccessException("E-mail ou senha inválidos.");
            }

            // Login bem-sucedido — resetar tentativas
            if (usuario.TentativasLoginFalhas > 0)
            {
                usuario.TentativasLoginFalhas = 0;
                usuario.ContaBloqueada = false;
                usuario.BloqueioAte = null;
                await _usuarioRepository.UpdateAsync(usuario);
            }

            return new AuthResponseDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Role = usuario.Role.ToString(),
                Token = GenerateJwtToken(usuario)
            };
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDTO request, string? baseUrl = null)
        {
            var usuario = await _usuarioRepository.GetByEmailAsync(request.Email);

            // SEGURANÇA: sempre retornar sucesso (não revelar se email existe)
            if (usuario == null)
            {
                _logger.LogInformation("Tentativa de recuperação de senha para email inexistente: {Email}", request.Email);
                return;
            }

            // Gerar token de reset
            var resetToken = Guid.NewGuid().ToString("N");
            usuario.ResetToken = resetToken;
            usuario.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

            await _usuarioRepository.UpdateAsync(usuario);

            // Enviar email
            await _emailService.SendPasswordResetEmailAsync(usuario.Email, usuario.Nome, resetToken, baseUrl);

            _logger.LogInformation("Token de recuperação de senha gerado para: {Email}", usuario.Email);
        }

        public async Task ResetPasswordAsync(ResetPasswordDTO request)
        {
            var usuario = await _usuarioRepository.GetByResetTokenAsync(request.Token);

            if (usuario == null)
                throw new InvalidOperationException("Token de recuperação inválido ou expirado.");

            // Atualizar senha
            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaSenha, workFactor: 12);

            // Limpar token de reset e desbloquear conta
            usuario.ResetToken = null;
            usuario.ResetTokenExpiry = null;
            usuario.ContaBloqueada = false;
            usuario.TentativasLoginFalhas = 0;
            usuario.BloqueioAte = null;

            await _usuarioRepository.UpdateAsync(usuario);

            _logger.LogInformation("Senha redefinida com sucesso para: {Email}", usuario.Email);
        }

        /// <summary>
        /// Gera um JWT com claims de identidade e role.
        /// Token expira em 8 horas (reduzido de 24h por segurança).
        /// </summary>
        private string GenerateJwtToken(Usuario usuario)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Role, usuario.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
