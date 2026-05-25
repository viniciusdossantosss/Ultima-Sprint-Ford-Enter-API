using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            string? nivelPedagogico = null;
            string? modalidadeSugerida = null;
            string? nomeResponsavel = null;
            string? telefoneResponsavel = null;

            if (role == UsuarioRole.Aluno)
            {
                if (!dto.DataNascimento.HasValue)
                    throw new ArgumentException("Data de nascimento é obrigatória para alunos.");

                if (dto.DataNascimento.Value > DateTime.UtcNow)
                    throw new ArgumentException("Data de nascimento inválida.");

                if (string.IsNullOrWhiteSpace(dto.Telefone))
                    throw new ArgumentException("Telefone do aluno é obrigatório.");

                // Calcular idade
                var dataNasc = dto.DataNascimento.Value;
                var hoje = DateTime.Today;
                var idade = hoje.Year - dataNasc.Year;
                if (dataNasc.Date > hoje.AddYears(-idade)) idade--;

                if (idade < 18)
                {
                    if (string.IsNullOrWhiteSpace(dto.NomeResponsavel))
                        throw new ArgumentException("Nome do responsável legal é obrigatório para alunos menores de idade.");

                    if (string.IsNullOrWhiteSpace(dto.TelefoneResponsavel))
                        throw new ArgumentException("Telefone do responsável legal é obrigatório para alunos menores de idade.");

                    nomeResponsavel = dto.NomeResponsavel;
                    telefoneResponsavel = dto.TelefoneResponsavel;
                }

                nivelPedagogico = "Iniciante";

                // Sugerir modalidade baseada na idade
                if (idade < 12)
                    modalidadeSugerida = "Infantil";
                else if (idade >= 60)
                    modalidadeSugerida = "Hidroginástica";
                else
                    modalidadeSugerida = "Aula Normal";
            }

            var senhaTemporaria = !string.IsNullOrWhiteSpace(dto.Senha)
            ? dto.Senha
            : GerarSenhaAleatoria();

            var usuario = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(senhaTemporaria, workFactor: 12),
                Role = role,
                DataCriacao = DateTime.UtcNow,
                DataNascimento = role == UsuarioRole.Aluno ? dto.DataNascimento : null,
                NivelPedagogico = nivelPedagogico,
                ModalidadeSugerida = modalidadeSugerida,
                Telefone = role == UsuarioRole.Aluno ? dto.Telefone : null,
                NomeResponsavel = nomeResponsavel,
                TelefoneResponsavel = telefoneResponsavel,
                DocumentacaoSaudeEntregue = role == UsuarioRole.Aluno && dto.DocumentacaoSaudeEntregue,
                ProblemasSaude = role == UsuarioRole.Aluno ? dto.ProblemasSaude : null,

                // Campos de Professor
                Cref = role == UsuarioRole.Professor ? dto.Cref : null,
                CrefAtivo = role == UsuarioRole.Professor && dto.CrefAtivo,
                AptoBebes = role == UsuarioRole.Professor && dto.AptoBebes,
                AptoInfantil = role == UsuarioRole.Professor && dto.AptoInfantil,
                AptoAdulto = role == UsuarioRole.Professor && dto.AptoAdulto,
                AptoAltaPerformance = role == UsuarioRole.Professor && dto.AptoAltaPerformance,
                AptoHidroginastica = role == UsuarioRole.Professor && dto.AptoHidroginastica,
                AptoPcd = role == UsuarioRole.Professor && dto.AptoPcd,
                ValidadeSalvamentoAquatico = role == UsuarioRole.Professor ? dto.ValidadeSalvamentoAquatico : null,
                ValidadePrimeirosSocorros = role == UsuarioRole.Professor ? dto.ValidadePrimeirosSocorros : null
            };

            await _usuarioRepository.CreateAsync(usuario);

            // Enviar email de boas-vindas (async, não bloqueia se falhar)
            _ = _emailService.SendWelcomeEmailAsync(dto.Email, dto.Nome, senhaTemporaria);

            return MapToDto(usuario);
        }

        public async Task<UsuarioResponseDTO?> UpdateAsync(int id, UsuarioUpdateDTO dto)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null) return null;

            // Verificar email duplicado (se o e-mail mudou)
            if (usuario.Email != dto.Email && await _usuarioRepository.EmailExistsAsync(dto.Email))
                throw new InvalidOperationException("Este e-mail já está cadastrado por outro usuário.");

            usuario.Nome = dto.Nome;
            usuario.Email = dto.Email;

            if (usuario.Role == UsuarioRole.Aluno)
            {
                if (!dto.DataNascimento.HasValue)
                    throw new ArgumentException("Data de nascimento é obrigatória para alunos.");

                if (dto.DataNascimento.Value > DateTime.UtcNow)
                    throw new ArgumentException("Data de nascimento inválida.");

                if (string.IsNullOrWhiteSpace(dto.Telefone))
                    throw new ArgumentException("Telefone do aluno é obrigatório.");

                // Calcular idade
                var dataNasc = dto.DataNascimento.Value;
                var hoje = DateTime.Today;
                var idade = hoje.Year - dataNasc.Year;
                if (dataNasc.Date > hoje.AddYears(-idade)) idade--;

                if (idade < 18)
                {
                    if (string.IsNullOrWhiteSpace(dto.NomeResponsavel))
                        throw new ArgumentException("Nome do responsável legal é obrigatório para alunos menores de idade.");

                    if (string.IsNullOrWhiteSpace(dto.TelefoneResponsavel))
                        throw new ArgumentException("Telefone do responsável legal é obrigatório para alunos menores de idade.");

                    usuario.NomeResponsavel = dto.NomeResponsavel;
                    usuario.TelefoneResponsavel = dto.TelefoneResponsavel;
                }
                else
                {
                    usuario.NomeResponsavel = null;
                    usuario.TelefoneResponsavel = null;
                }

                usuario.DataNascimento = dto.DataNascimento;
                usuario.Telefone = dto.Telefone;
                usuario.DocumentacaoSaudeEntregue = dto.DocumentacaoSaudeEntregue;
                usuario.ProblemasSaude = dto.ProblemasSaude;
                usuario.NivelPedagogico = dto.NivelPedagogico;

                // Sugerir modalidade baseada na idade
                if (idade < 12)
                    usuario.ModalidadeSugerida = "Infantil";
                else if (idade >= 60)
                    usuario.ModalidadeSugerida = "Hidroginástica";
                else
                    usuario.ModalidadeSugerida = "Aula Normal";
            }
            else if (usuario.Role == UsuarioRole.Professor)
            {
                usuario.Cref = dto.Cref;
                usuario.CrefAtivo = dto.CrefAtivo;
                usuario.AptoBebes = dto.AptoBebes;
                usuario.AptoInfantil = dto.AptoInfantil;
                usuario.AptoAdulto = dto.AptoAdulto;
                usuario.AptoAltaPerformance = dto.AptoAltaPerformance;
                usuario.AptoHidroginastica = dto.AptoHidroginastica;
                usuario.AptoPcd = dto.AptoPcd;
                usuario.ValidadeSalvamentoAquatico = dto.ValidadeSalvamentoAquatico;
                usuario.ValidadePrimeirosSocorros = dto.ValidadePrimeirosSocorros;
            }

            await _usuarioRepository.UpdateAsync(usuario);
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

        public async Task<UsuarioResponseDTO?> UpdatePerfilAsync(int id, PerfilUpdateDTO dto)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null) return null;

            // Verificar email duplicado (se o e-mail mudou)
            if (usuario.Email != dto.Email && await _usuarioRepository.EmailExistsAsync(dto.Email))
                throw new InvalidOperationException("Este e-mail já está cadastrado por outro usuário.");

            usuario.Nome = dto.Nome;
            usuario.Email = dto.Email;

            if (usuario.Role == UsuarioRole.Aluno)
            {
                if (!dto.DataNascimento.HasValue)
                    throw new ArgumentException("Data de nascimento é obrigatória para alunos.");

                if (dto.DataNascimento.Value > DateTime.UtcNow)
                    throw new ArgumentException("Data de nascimento inválida.");

                if (string.IsNullOrWhiteSpace(dto.Telefone))
                    throw new ArgumentException("Telefone do aluno é obrigatório.");

                // Calcular idade
                var dataNasc = dto.DataNascimento.Value;
                var hoje = DateTime.Today;
                var idade = hoje.Year - dataNasc.Year;
                if (dataNasc.Date > hoje.AddYears(-idade)) idade--;

                if (idade < 18)
                {
                    if (string.IsNullOrWhiteSpace(dto.NomeResponsavel))
                        throw new ArgumentException("Nome do responsável legal é obrigatório para alunos menores de idade.");

                    if (string.IsNullOrWhiteSpace(dto.TelefoneResponsavel))
                        throw new ArgumentException("Telefone do responsável legal é obrigatório para alunos menores de idade.");

                    usuario.NomeResponsavel = dto.NomeResponsavel;
                    usuario.TelefoneResponsavel = dto.TelefoneResponsavel;
                }
                else
                {
                    usuario.NomeResponsavel = null;
                    usuario.TelefoneResponsavel = null;
                }

                usuario.DataNascimento = dto.DataNascimento;
                usuario.Telefone = dto.Telefone;
                usuario.ProblemasSaude = dto.ProblemasSaude;

                // Sugerir modalidade baseada na idade
                if (idade < 12)
                    usuario.ModalidadeSugerida = "Infantil";
                else if (idade >= 60)
                    usuario.ModalidadeSugerida = "Hidroginástica";
                else
                    usuario.ModalidadeSugerida = "Aula Normal";
            }
            else if (usuario.Role == UsuarioRole.Professor)
            {
                usuario.Cref = dto.Cref;
                if (dto.CrefAtivo.HasValue)
                {
                    usuario.CrefAtivo = dto.CrefAtivo.Value;
                }
                if (dto.AptoBebes.HasValue) usuario.AptoBebes = dto.AptoBebes.Value;
                if (dto.AptoInfantil.HasValue) usuario.AptoInfantil = dto.AptoInfantil.Value;
                if (dto.AptoAdulto.HasValue) usuario.AptoAdulto = dto.AptoAdulto.Value;
                if (dto.AptoAltaPerformance.HasValue) usuario.AptoAltaPerformance = dto.AptoAltaPerformance.Value;
                if (dto.AptoHidroginastica.HasValue) usuario.AptoHidroginastica = dto.AptoHidroginastica.Value;
                if (dto.AptoPcd.HasValue) usuario.AptoPcd = dto.AptoPcd.Value;
                
                usuario.ValidadeSalvamentoAquatico = dto.ValidadeSalvamentoAquatico;
                usuario.ValidadePrimeirosSocorros = dto.ValidadePrimeirosSocorros;
            }

            // Atualização de senha se informada
            if (!string.IsNullOrWhiteSpace(dto.SenhaAtual))
            {
                if (string.IsNullOrWhiteSpace(dto.NovaSenha))
                    throw new ArgumentException("Para alterar a senha, você deve informar a nova senha.");

                // Validar senha atual
                if (!BCrypt.Net.BCrypt.Verify(dto.SenhaAtual, usuario.SenhaHash))
                    throw new InvalidOperationException("Senha atual incorreta.");

                usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha, workFactor: 12);
                
                // Limpar bloqueios se houver
                usuario.TentativasLoginFalhas = 0;
                usuario.ContaBloqueada = false;
                usuario.BloqueioAte = null;
            }
            else if (!string.IsNullOrWhiteSpace(dto.NovaSenha))
            {
                throw new ArgumentException("Para definir uma nova senha, você deve informar a senha atual.");
            }

            await _usuarioRepository.UpdateAsync(usuario);
            return MapToDto(usuario);
        }

        public async Task<UsuarioResponseDTO?> UpdateNivelAsync(int id, string nivelPedagogico)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null) return null;

            if (usuario.Role != UsuarioRole.Aluno)
                throw new InvalidOperationException("Apenas o nível pedagógico de alunos pode ser alterado.");

            usuario.NivelPedagogico = nivelPedagogico;

            await _usuarioRepository.UpdateAsync(usuario);
            return MapToDto(usuario);
        }

        public async Task<IEnumerable<UsuarioResponseDTO>> GetAlertasAsync()
        {
            var usuarios = await _usuarioRepository.GetAllAsync();
            var hoje = DateTime.Today;
            var trintaDias = hoje.AddDays(30);

            var professoresComAlerta = usuarios
                .Where(u => u.Role == UsuarioRole.Professor &&
                            ((u.ValidadeSalvamentoAquatico.HasValue && u.ValidadeSalvamentoAquatico.Value <= trintaDias) ||
                             (u.ValidadePrimeirosSocorros.HasValue && u.ValidadePrimeirosSocorros.Value <= trintaDias)))
                .Select(u => MapToDto(u));

            return professoresComAlerta;
        }

        private static string GerarSenhaAleatoria()
        {
            const string maiusculas = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string minusculas = "abcdefghijklmnopqrstuvwxyz";
            const string numeros = "0123456789";
            const string especiais = "@$!%*?&#";
            
            var random = new Random();
            var caracteres = new char[12];
            
            // Garantir pelo menos um de cada grupo para passar no regex de validação
            caracteres[0] = maiusculas[random.Next(maiusculas.Length)];
            caracteres[1] = minusculas[random.Next(minusculas.Length)];
            caracteres[2] = numeros[random.Next(numeros.Length)];
            caracteres[3] = especiais[random.Next(especiais.Length)];
            
            string todos = maiusculas + minusculas + numeros + especiais;
            for (int i = 4; i < 12; i++)
            {
                caracteres[i] = todos[random.Next(todos.Length)];
            }
            
            // Embaralhar
            return new string(caracteres.OrderBy(x => random.Next()).ToArray());
        }

        private static UsuarioResponseDTO MapToDto(Usuario u) => new()
        {
            Id = u.Id,
            Nome = u.Nome,
            Email = u.Email,
            Role = u.Role.ToString(),
            DataCriacao = u.DataCriacao,
            DataNascimento = u.DataNascimento,
            NivelPedagogico = u.NivelPedagogico,
            ModalidadeSugerida = u.ModalidadeSugerida,
            Telefone = u.Telefone,
            NomeResponsavel = u.NomeResponsavel,
            TelefoneResponsavel = u.TelefoneResponsavel,
            DocumentacaoSaudeEntregue = u.DocumentacaoSaudeEntregue,
            ProblemasSaude = u.ProblemasSaude,
            Cref = u.Cref,
            CrefAtivo = u.CrefAtivo,
            AptoBebes = u.AptoBebes,
            AptoInfantil = u.AptoInfantil,
            AptoAdulto = u.AptoAdulto,
            AptoAltaPerformance = u.AptoAltaPerformance,
            AptoHidroginastica = u.AptoHidroginastica,
            AptoPcd = u.AptoPcd,
            ValidadeSalvamentoAquatico = u.ValidadeSalvamentoAquatico,
            ValidadePrimeirosSocorros = u.ValidadePrimeirosSocorros
        };
    }
}

