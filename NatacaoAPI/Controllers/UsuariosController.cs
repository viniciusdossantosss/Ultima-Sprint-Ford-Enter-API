using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NatacaoAPI.DTOs.Usuario;
using NatacaoAPI.Services.Interfaces;

namespace NatacaoAPI.Controllers
{
    /// <summary>
    /// Controller de gerenciamento de usuários.
    /// Ações administrativas exigem papel de Admin; a atualização de perfil é permitida para qualquer usuário autenticado.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        /// <summary>
        /// Lista todos os usuários do sistema (exclusivo Admin).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<UsuarioResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var usuarios = await _usuarioService.GetAllAsync();
            return Ok(usuarios);
        }

        /// <summary>
        /// Lista todos os alunos do sistema. Acessível por Admin e Professor.
        /// </summary>
        [HttpGet("alunos")]
        [Authorize(Roles = "Admin,Professor")]
        [ProducesResponseType(typeof(IEnumerable<UsuarioResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAlunos()
        {
            var usuarios = await _usuarioService.GetAllAsync();
            var alunos = usuarios.Where(u => u.Role == "Aluno");
            return Ok(alunos);
        }

        /// <summary>
        /// Retorna um usuário pelo ID (exclusivo Admin).
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(UsuarioResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            if (usuario == null)
                return NotFound(new { message = "Usuário não encontrado." });
            return Ok(usuario);
        }

        /// <summary>
        /// Cria um novo usuário (Aluno ou Professor) (exclusivo Admin).
        /// Envia email de boas-vindas com credenciais.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(UsuarioResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] UsuarioCreateDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _usuarioService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza as informações de um usuário (Aluno ou Professor) (exclusivo Admin).
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(UsuarioResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _usuarioService.UpdateAsync(id, request);
                if (result == null)
                    return NotFound(new { message = "Usuário não encontrado." });

                return Ok(result);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza o nível pedagógico de um Aluno (Professor ou Admin).
        /// </summary>
        [HttpPut("{id:int}/nivel")]
        [Authorize(Roles = "Admin,Professor")]
        [ProducesResponseType(typeof(UsuarioResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNivel(int id, [FromBody] AlunoNivelUpdateDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _usuarioService.UpdateNivelAsync(id, request.NivelPedagogico);
                if (result == null)
                    return NotFound(new { message = "Usuário não encontrado." });

                return Ok(result);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Remove um usuário do sistema (não permite excluir Admin) (exclusivo Admin).
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _usuarioService.DeleteAsync(id);
                if (!result)
                    return NotFound(new { message = "Usuário não encontrado." });
                return NoContent();
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Retorna o perfil do próprio usuário autenticado.
        /// </summary>
        [HttpGet("perfil")]
        [ProducesResponseType(typeof(UsuarioResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPerfil()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { message = "Usuário não autenticado." });

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return BadRequest(new { message = "ID de usuário inválido no token." });

            var usuario = await _usuarioService.GetByIdAsync(userId);
            if (usuario == null)
                return NotFound(new { message = "Usuário não encontrado." });

            return Ok(usuario);
        }

        /// <summary>
        /// Atualiza o próprio perfil do usuário autenticado (Aluno, Professor ou Admin).
        /// </summary>
        [HttpPut("perfil")]
        [ProducesResponseType(typeof(UsuarioResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePerfil([FromBody] PerfilUpdateDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { message = "Usuário não autenticado." });

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return BadRequest(new { message = "ID de usuário inválido no token." });

            try
            {
                var result = await _usuarioService.UpdatePerfilAsync(userId, request);
                if (result == null)
                    return NotFound(new { message = "Usuário não encontrado." });

                return Ok(result);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lista os professores com certificações próximas ao vencimento ou vencidas (Admin apenas).
        /// </summary>
        [HttpGet("alertas")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<UsuarioResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAlertas()
        {
            var alertas = await _usuarioService.GetAlertasAsync();
            return Ok(alertas);
        }
    }
}

