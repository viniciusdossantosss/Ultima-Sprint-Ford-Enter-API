using NatacaoAPI.DTOs.Usuario;
using NatacaoAPI.Models;
using NatacaoAPI.Repositories.Interfaces;
using NatacaoAPI.Services.Interfaces;

namespace NatacaoAPI.Services
{
    /// <summary>
    /// Serviço de gerenciamento de usuários (somente Admin).
    /// Responsável por criar alunos e professores, enviar email de boas-vindas.
    /// </summary>
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEmailService _emailService;

        public UsuarioService(IUsuarioRepository usuarioRepository, IEmailService emailService)
        {
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
        }

        public async Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync()
        {
            var usuarios = await _usuarioRepository.GetAllAsync();
            return usuarios.Select(u => MapToDto(u));
        }

        public async Task<UsuarioResponseDTO?> GetByIdAsync(int id)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null) return null;
            return MapToDto(usuario);
        }

        public async Task<UsuarioResponseDTO> CreateAsync(UsuarioCreateDTO dto)
        {
            // Verificar email duplicado
            if (await _usuarioRepository.EmailExistsAsync(dto.Email))
                throw new InvalidOperationException("Este e-mail já está cadastrado.");

            // Validar role — Admin não pode criar outros Admins
            if (!Enum.TryParse<UsuarioRole>(dto.Role, true, out var role) ||
                role == UsuarioRole.Admin)
                throw new ArgumentException("Role inválida. Use 'Aluno' ou 'Professor'.");

            var usuario = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha, workFactor: 12),
                Role = role,
                DataCriacao = DateTime.UtcNow
            };

            await _usuarioRepository.CreateAsync(usuario);

            // Enviar email de boas-vindas (async, não bloqueia se falhar)
            _ = _emailService.SendWelcomeEmailAsync(dto.Email, dto.Nome, dto.Senha);

            return MapToDto(usuario);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null) return false;

            // Proteger contra exclusão de Admin
            if (usuario.Role == UsuarioRole.Admin)
                throw new InvalidOperationException("Não é permitido excluir o usuário administrador.");

            await _usuarioRepository.DeleteAsync(usuario);
            return true;
        }

        private static UsuarioResponseDTO MapToDto(Usuario u) => new()
        {
            Id = u.Id,
            Nome = u.Nome,
            Email = u.Email,
            Role = u.Role.ToString(),
            DataCriacao = u.DataCriacao
        };
    }
}
