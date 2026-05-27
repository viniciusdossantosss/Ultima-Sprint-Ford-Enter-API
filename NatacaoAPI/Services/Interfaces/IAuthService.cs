using NatacaoAPI.DTOs.Auth;

namespace NatacaoAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> LoginAsync(LoginRequestDTO request);
        Task ForgotPasswordAsync(ForgotPasswordDTO request, string? baseUrl = null);
        Task ResetPasswordAsync(ResetPasswordDTO request);
    }
}
