using NatacaoAPI.DTOs.Usuario;
using NatacaoAPI.Models;

namespace NatacaoAPI.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync();
        Task<UsuarioResponseDTO?> GetByIdAsync(int id);
        Task<UsuarioResponseDTO> CreateAsync(UsuarioCreateDTO dto);
        Task<UsuarioResponseDTO?> UpdateAsync(int id, UsuarioUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<UsuarioResponseDTO?> UpdatePerfilAsync(int id, PerfilUpdateDTO dto);
        Task<IEnumerable<UsuarioResponseDTO>> GetAlertasAsync();
        Task<UsuarioResponseDTO?> UpdateNivelAsync(int id, string nivelPedagogico);
    }
}

