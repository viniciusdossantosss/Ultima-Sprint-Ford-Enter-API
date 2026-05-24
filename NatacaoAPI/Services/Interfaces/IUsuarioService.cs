using NatacaoAPI.DTOs.Usuario;

namespace NatacaoAPI.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync();
        Task<UsuarioResponseDTO?> GetByIdAsync(int id);
        Task<UsuarioResponseDTO> CreateAsync(UsuarioCreateDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
