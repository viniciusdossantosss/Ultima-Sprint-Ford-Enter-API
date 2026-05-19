using AutoMapper;
using NatacaoAPI.Models;
using NatacaoAPI.DTOs.Reserva;

namespace NatacaoAPI.Profiles
{
    /// <summary>
    /// Perfil AutoMapper para Reserva.
    /// Desnormaliza dados de Aluno e Turma no DTO de resposta para
    /// que o frontend tenha todas as informações em uma única chamada.
    /// </summary>
    public class ReservaProfile : Profile
    {
        public ReservaProfile()
        {
            CreateMap<Reserva, ReservaResponseDTO>()
                .ForMember(dest => dest.AlunoNome,
                    opt => opt.MapFrom(src => src.Aluno != null ? src.Aluno.Nome : ""))
                .ForMember(dest => dest.TurmaNome,
                    opt => opt.MapFrom(src => src.Turma != null ? src.Turma.Nome : ""))
                .ForMember(dest => dest.DiaSemana,
                    opt => opt.MapFrom(src => src.Turma != null ? src.Turma.DiaSemana.ToString() : ""))
                .ForMember(dest => dest.HorarioInicio,
                    opt => opt.MapFrom(src => src.Turma != null ? src.Turma.HorarioInicio.ToString(@"hh\:mm") : ""))
                .ForMember(dest => dest.HorarioFim,
                    opt => opt.MapFrom(src => src.Turma != null ? src.Turma.HorarioFim.ToString(@"hh\:mm") : ""))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}
