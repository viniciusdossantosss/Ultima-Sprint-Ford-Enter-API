using NatacaoAPI.Models;

namespace NatacaoAPI.Repositories.Interfaces
{
    /// <summary>
    /// Contrato de acesso a dados para Usuários.
    /// Não expõe IQueryable — apenas métodos específicos de negócio.
    /// </summary>
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByIdAsync(int id);
        Task<Usuario?> GetByEmailAsync(string email);
        Task<Usuario> CreateAsync(Usuario usuario);
        Task<bool> EmailExistsAsync(string email);
    }
}
