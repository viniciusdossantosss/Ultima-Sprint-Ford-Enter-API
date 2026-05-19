namespace NatacaoAPI.DTOs.Turma
{
    /// <summary>
    /// DTO de resposta para turmas. Inclui VagasDisponiveis calculado
    /// dinamicamente pelo Service (CapacidadeMaxima - reservas ativas).
    /// </summary>
    public class TurmaResponseDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Modalidade { get; set; } = string.Empty;
        public string DiaSemana { get; set; } = string.Empty;
        public string HorarioInicio { get; set; } = string.Empty;
        public string HorarioFim { get; set; } = string.Empty;
        public int CapacidadeMaxima { get; set; }
        public int VagasDisponiveis { get; set; }
        public int ProfessorId { get; set; }
        public string ProfessorNome { get; set; } = string.Empty;
    }
}
