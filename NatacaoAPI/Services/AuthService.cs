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
    /// Serviço de autenticação responsável por:
    /// - Registro com hash BCrypt da senha
    /// - Login com verificação BCrypt + geração de JWT
    /// 
    /// Decisão: o JWT contém claims de Id, Email e Role para que
    /// o middleware de autorização possa validar perfis sem consultar o banco.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUsuarioRepository usuarioRepository, IConfiguration configuration)
        {
            _usuarioRepository = usuarioRepository;
            _configuration = configuration;
        }

        public async Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO request)
        {
            // Verificar se o e-mail já está em uso
            if (await _usuarioRepository.EmailExistsAsync(request.Email))
                throw new InvalidOperationException("Este e-mail já está cadastrado.");

            // Validar role
            if (!Enum.TryParse<UsuarioRole>(request.Role, true, out var role))
                throw new ArgumentException("Role inválida. Use 'Aluno' ou 'Professor'.");

            // Criar o usuário com senha hashada via BCrypt
            var usuario = new Usuario
            {
                Nome = request.Nome,
                Email = request.Email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
                Role = role,
                DataCriacao = DateTime.UtcNow
            };

            await _usuarioRepository.CreateAsync(usuario);

            // Retornar resposta com token JWT
            return new AuthResponseDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Role = usuario.Role.ToString(),
                Token = GenerateJwtToken(usuario)
            };
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            var usuario = await _usuarioRepository.GetByEmailAsync(request.Email);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
                throw new UnauthorizedAccessException("E-mail ou senha inválidos.");

            return new AuthResponseDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Role = usuario.Role.ToString(),
                Token = GenerateJwtToken(usuario)
            };
        }

        /// <summary>
        /// Gera um JWT com claims de identidade e role.
        /// O token expira em 24 horas para balancear segurança e UX.
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
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
