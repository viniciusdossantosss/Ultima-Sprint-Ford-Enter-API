using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NatacaoAPI.DTOs.Reserva;
using NatacaoAPI.Services.Interfaces;

namespace NatacaoAPI.Controllers
{
    /// <summary>
    /// Controller de Reservas.
    /// 
    /// Fluxo de autorização:
    /// - Professor: pode listar TODAS as reservas (GET /api/reservas)
    /// - Aluno: só vê suas próprias reservas, cria e cancela reservas
    /// 
    /// As regras de negócio RF001/RF002 são validadas no ReservaService.
    /// Se violadas, o Service lança exceção → GlobalExceptionMiddleware → 400.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservasController : ControllerBase
    {
        private readonly IReservaService _reservaService;

        public ReservasController(IReservaService reservaService)
        {
            _reservaService = reservaService;
        }

        /// <summary>
        /// Lista reservas.
        /// Professor: retorna todas. Aluno: retorna apenas as suas.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ReservaResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = GetUserId();

            if (role == "Professor")
            {
                var todas = await _reservaService.GetAllAsync();
                return Ok(todas);
            }
            else
            {
                var minhas = await _reservaService.GetByAlunoIdAsync(userId);
                return Ok(minhas);
            }
        }

        /// <summary>
        /// Retorna detalhes de uma reserva específica.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ReservaResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var reserva = await _reservaService.GetByIdAsync(id);
            if (reserva == null) return NotFound(new { message = "Reserva não encontrada." });

            // Aluno só pode ver suas próprias reservas
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = GetUserId();
            if (role != "Professor" && reserva.AlunoId != userId)
                return Forbid();

            return Ok(reserva);
        }

        /// <summary>
        /// Cria uma nova reserva. Apenas Alunos.
        /// O AlunoId é extraído do token JWT (segurança por design).
        /// RF001 e RF002 são validados no Service.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Aluno")]
        [ProducesResponseType(typeof(ReservaResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] ReservaCreateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var alunoId = GetUserId();
            var reserva = await _reservaService.CreateAsync(dto, alunoId);
            return CreatedAtAction(nameof(GetById), new { id = reserva.Id }, reserva);
        }

        /// <summary>
        /// Cancela (soft delete) uma reserva. Apenas o aluno dono.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Aluno")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Cancel(int id)
        {
            var alunoId = GetUserId();
            var cancelled = await _reservaService.CancelAsync(id, alunoId);
            if (!cancelled) return NotFound(new { message = "Reserva não encontrada." });
            return NoContent();
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(claim!.Value);
        }
    }
}
