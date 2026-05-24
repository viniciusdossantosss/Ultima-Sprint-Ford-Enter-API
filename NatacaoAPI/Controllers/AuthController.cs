using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NatacaoAPI.DTOs.Auth;
using NatacaoAPI.Services.Interfaces;

namespace NatacaoAPI.Controllers
{
    /// <summary>
    /// Controller de autenticação — rotas públicas (sem [Authorize]).
    /// Registro público removido — apenas Admin pode criar usuários via /api/usuarios.
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
        /// Realiza login e retorna um token JWT.
        /// Bloqueado após 5 tentativas falhas por 15 minutos.
        /// </summary>
        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(AuthResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Solicita recuperação de senha. Envia email com link de reset.
        /// Sempre retorna 200 para não revelar se o email existe.
        /// </summary>
        [HttpPost("forgot-password")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _authService.ForgotPasswordAsync(request);
            return Ok(new { message = "Se o e-mail estiver cadastrado, você receberá um link de recuperação." });
        }

        /// <summary>
        /// Redefine a senha usando o token recebido por email.
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _authService.ResetPasswordAsync(request);
            return Ok(new { message = "Senha redefinida com sucesso." });
        }
    }
}
