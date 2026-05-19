using System.ComponentModel.DataAnnotations;

namespace NatacaoAPI.DTOs.Reserva
{
    /// <summary>
    /// DTO de criação de reserva. Apenas TurmaId é necessário;
    /// o AlunoId é extraído do token JWT pelo Controller.
    /// Decisão: não expor AlunoId no body para impedir que um aluno
    /// crie reservas em nome de outro (segurança por design).
    /// </summary>
    public class ReservaCreateDTO
    {
        [Required(ErrorMessage = "O ID da turma é obrigatório.")]
        public int TurmaId { get; set; }
    }
}
