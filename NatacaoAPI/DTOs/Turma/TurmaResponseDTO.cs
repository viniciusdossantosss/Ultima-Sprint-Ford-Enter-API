using System.Collections.Generic;

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
        public DateTime DataHoraInicio { get; set; }
        public DateTime DataHoraFim { get; set; }
        public int CapacidadeMaxima { get; set; }
        public int VagasDisponiveis { get; set; }
        public int ProfessorId { get; set; }
        public string ProfessorNome { get; set; } = string.Empty;
        public bool ProfessorCertificacaoExpirada { get; set; }
        public string? ProfessorInconformidadeMensagem { get; set; }
        public List<AlunoInscritoDTO> AlunosInscritos { get; set; } = new();
    }
}