using AutoMapper;
using NatacaoAPI.Models;
using NatacaoAPI.DTOs.Turma;

namespace NatacaoAPI.Profiles
{
    /// <summary>
    /// Perfil AutoMapper para mapeamento bidirecional entre Turma e seus DTOs.
    /// 
    /// Decisão: converter HorarioInicio/HorarioFim de string (DTO) para TimeSpan (Model)
    /// no mapeamento, pois o frontend trabalha com strings "HH:mm" enquanto o domínio
    /// usa TimeSpan para cálculos de conflito de horário.
    /// </summary>
    public class TurmaProfile : Profile
    {
        public TurmaProfile()
        {
            // DTO de criação → Entidade
            CreateMap<TurmaCreateDTO, Turma>()
                .ForMember(dest => dest.DiaSemana,
                    opt => opt.MapFrom(src => (DiaSemana)src.DiaSemana))
                .ForMember(dest => dest.HorarioInicio,
                    opt => opt.MapFrom(src => TimeSpan.Parse(src.HorarioInicio)))
                .ForMember(dest => dest.HorarioFim,
                    opt => opt.MapFrom(src => TimeSpan.Parse(src.HorarioFim)));

            // DTO de atualização → Entidade
            CreateMap<TurmaUpdateDTO, Turma>()
                .ForMember(dest => dest.DiaSemana,
                    opt => opt.MapFrom(src => (DiaSemana)src.DiaSemana))
                .ForMember(dest => dest.HorarioInicio,
                    opt => opt.MapFrom(src => TimeSpan.Parse(src.HorarioInicio)))
                .ForMember(dest => dest.HorarioFim,
                    opt => opt.MapFrom(src => TimeSpan.Parse(src.HorarioFim)));

            // Entidade → DTO de resposta
            CreateMap<Turma, TurmaResponseDTO>()
                .ForMember(dest => dest.DiaSemana,
                    opt => opt.MapFrom(src => src.DiaSemana.ToString()))
                .ForMember(dest => dest.HorarioInicio,
                    opt => opt.MapFrom(src => src.HorarioInicio.ToString(@"hh\:mm")))
                .ForMember(dest => dest.HorarioFim,
                    opt => opt.MapFrom(src => src.HorarioFim.ToString(@"hh\:mm")))
                .ForMember(dest => dest.ProfessorNome,
                    opt => opt.MapFrom(src => src.Professor != null ? src.Professor.Nome : ""))
                .ForMember(dest => dest.VagasDisponiveis,
                    opt => opt.Ignore()); // Calculado dinamicamente no Service
        }
    }
}
