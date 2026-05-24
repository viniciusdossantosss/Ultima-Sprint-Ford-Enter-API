using Mapster;
using NatacaoAPI.DTOs.Reserva;
using NatacaoAPI.DTOs.Turma;
using NatacaoAPI.Models;

namespace NatacaoAPI.Profiles
{
    /// <summary>
    /// Configuração centralizada do Mapster (substitui os antigos AutoMapper Profiles).
    /// Registra todos os mapeamentos customizados entre Models e DTOs.
    /// 
    /// Migração: AutoMapper 13.0.1 → Mapster 7.4.0
    /// Motivo: CVE-2026-32933 (DoS via recursão descontrolada no AutoMapper)
    /// </summary>
    public static class MapsterConfig
    {
        public static void RegisterMappings()
        {
            // ─── TurmaCreateDTO → Turma ─────────────────────────────
            TypeAdapterConfig<TurmaCreateDTO, Turma>.NewConfig();

            // ─── TurmaUpdateDTO → Turma ─────────────────────────────
            TypeAdapterConfig<TurmaUpdateDTO, Turma>.NewConfig();

            // ─── Turma → TurmaResponseDTO ───────────────────────────
            TypeAdapterConfig<Turma, TurmaResponseDTO>.NewConfig()
                .Map(dest => dest.ProfessorNome,
                    src => src.Professor != null ? src.Professor.Nome : "")
                .Ignore(dest => dest.VagasDisponiveis); // Calculado dinamicamente no Service

            // ─── Reserva → ReservaResponseDTO ───────────────────────
            TypeAdapterConfig<Reserva, ReservaResponseDTO>.NewConfig()
                .Map(dest => dest.AlunoNome,
                    src => src.Aluno != null ? src.Aluno.Nome : "")
                .Map(dest => dest.TurmaNome,
                    src => src.Turma != null ? src.Turma.Nome : "")
                .Map(dest => dest.DataHoraInicio,
                    src => src.Turma != null ? src.Turma.DataHoraInicio : default)
                .Map(dest => dest.DataHoraFim,
                    src => src.Turma != null ? src.Turma.DataHoraFim : default)
                .Map(dest => dest.Status,
                    src => src.Status.ToString());
        }
    }
}
