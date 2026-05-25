using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NatacaoAPI.DTOs.Turma;
using NatacaoAPI.Services.Interfaces;

namespace NatacaoAPI.Controllers
{
    /// <summary>
    /// Controller de Turmas.
    /// 
    /// RF003: Apenas Professores podem criar, editar e deletar turmas.
    /// Qualquer usuário autenticado pode listar turmas (GET).
    /// 
    /// O Controller é enxuto: apenas extrai dados da rota/body/token,
    /// chama o Service e padroniza a resposta HTTP.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TurmasController : ControllerBase
    {
        private readonly ITurmaService _turmaService;

        public TurmasController(ITurmaService turmaService)
        {
            _turmaService = turmaService;
        }

        /// <summary>
        /// Lista todas as turmas com vagas disponíveis.
        /// Acessível por qualquer usuário autenticado.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TurmaResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var turmas = await _turmaService.GetAllAsync();
            return Ok(turmas);
        }

        /// <summary>
        /// Retorna detalhes de uma turma específica.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TurmaResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var turma = await _turmaService.GetByIdAsync(id);
            if (turma == null) return NotFound(new { message = "Turma não encontrada." });
            return Ok(turma);
        }

        /// <summary>
        /// Cria uma nova turma. Somente Professor.
        /// O ProfessorId é extraído automaticamente do token JWT.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Professor,Admin")]
        [ProducesResponseType(typeof(TurmaResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] TurmaCreateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var professorId = GetUserId();
            var turma = await _turmaService.CreateAsync(dto, professorId);
            return CreatedAtAction(nameof(GetById), new { id = turma.Id }, turma);
        }

        /// <summary>
        /// Atualiza uma turma existente. Somente Professor.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Professor,Admin")]
        [ProducesResponseType(typeof(TurmaResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(int id, [FromBody] TurmaUpdateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var professorId = GetUserId();
            var turma = await _turmaService.UpdateAsync(id, dto, professorId);
            if (turma == null) return NotFound(new { message = "Turma não encontrada." });
            return Ok(turma);
        }

        /// <summary>
        /// Deleta uma turma. Somente Professor.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Professor,Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(int id)
        {
            var professorId = GetUserId();
            var deleted = await _turmaService.DeleteAsync(id, professorId);
            if (!deleted) return NotFound(new { message = "Turma não encontrada." });
            return NoContent();
        }

        /// <summary>
        /// Extrai o UserId do claim do token JWT.
        /// </summary>
        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(claim!.Value);
        }
    }
}
