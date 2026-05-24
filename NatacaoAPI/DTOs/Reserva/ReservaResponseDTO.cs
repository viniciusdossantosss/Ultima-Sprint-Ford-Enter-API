namespace NatacaoAPI.DTOs.Reserva
{
    /// <summary>
    /// DTO de resposta de reserva. Desnormaliza dados da Turma e do Aluno
    /// para que o frontend não precise fazer múltiplas requisições.
    /// </summary>
    public class ReservaResponseDTO
    {
        public int Id { get; set; }
        public int AlunoId { get; set; }
        public string AlunoNome { get; set; } = string.Empty;
        public int TurmaId { get; set; }
        public string TurmaNome { get; set; } = string.Empty;
        public DateTime DataHoraInicio { get; set; }
        public DateTime DataHoraFim { get; set; }
        public DateTime DataReserva { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}