using BinanceTradingBot.WebDashboard.Models;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Services
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthResponseDTO>> AuthenticateAsync(string username, string password);
        Task<ServiceResult<AuthResponseDTO>> RegisterUserAsync(RegisterDTO register);
        Task<ServiceResult> ChangePasswordAsync(string username, string currentPassword, string newPassword);
        Task<ServiceResult<AuthResponseDTO>> RefreshTokenAsync(string accessToken, string refreshToken);
        Task<ServiceResult> RevokeTokenAsync(string username);
    }
}