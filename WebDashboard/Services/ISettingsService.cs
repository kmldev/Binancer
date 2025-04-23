using BinanceTradingBot.WebDashboard.Models;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Services
{
    public interface ISettingsService
    {
        Task<AppSettingsDTO> GetSettingsAsync();
        Task<ServiceResult> UpdateSettingsAsync(AppSettingsDTO settings);
        Task<ServiceResult> UpdateRiskManagementSettingsAsync(RiskManagementSettingsDTO settings);
        Task<ServiceResult> UpdateApiCredentialsAsync(ApiCredentialsDTO credentials);
    }
}