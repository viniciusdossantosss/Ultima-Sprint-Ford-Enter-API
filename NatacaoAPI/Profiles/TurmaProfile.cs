using AutoMapper;
using NatacaoAPI.Models;
using NatacaoAPI.DTOs.Turma;

namespace NatacaoAPI.Profiles
{
    /// <summary>
    /// Perfil AutoMapper para mapeamento bidirecional entre Turma e seus DTOs.
    /// Agora trabalha com DataHoraInicio e DataHoraFim.
    /// </summary>
    public class TurmaProfile : Profile
    {
        public TurmaProfile()
        {
            // DTO de criação → Entidade
            CreateMap<TurmaCreateDTO, Turma>();

            // DTO de atualização → Entidade
            CreateMap<TurmaUpdateDTO, Turma>();

            // Entidade → DTO de resposta
            CreateMap<Turma, TurmaResponseDTO>()
                .ForMember(dest => dest.ProfessorNome,
                    opt => opt.MapFrom(src => src.Professor != null ? src.Professor.Nome : ""))
                .ForMember(dest => dest.VagasDisponiveis,
                    opt => opt.Ignore()); // Calculado dinamicamente no Service
        }
    }
}