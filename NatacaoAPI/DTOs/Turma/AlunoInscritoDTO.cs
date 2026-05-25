namespace NatacaoAPI.DTOs.Turma
{
    /// <summary>
    /// DTO que representa um aluno inscrito em uma turma específica.
    /// Contém informações úteis para exibição na agenda de aulas do professor.
    /// </summary>
    public class AlunoInscritoDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? NivelPedagogico { get; set; }
    }
}
