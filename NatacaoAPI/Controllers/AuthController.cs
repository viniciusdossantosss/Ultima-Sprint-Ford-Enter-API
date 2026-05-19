using Microsoft.AspNetCore.Mvc;
using NatacaoAPI.DTOs.Auth;
using NatacaoAPI.Services.Interfaces;

namespace NatacaoAPI.Controllers
{
    /// <summary>
    /// Controller de autenticação — rotas públicas (sem [Authorize]).
    /// Responsável apenas por receber a requisição e delegar ao AuthService.
    /// Nenhuma lógica de negócio aqui.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registra um novo usuário (Aluno ou Professor).
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(Register), new { id = result.Id }, result);
        }

        /// <summary>
        /// Realiza login e retorna um token JWT.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
    }
}
