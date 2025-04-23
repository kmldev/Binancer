using BinanceTradingBot.WebDashboard.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Services
{
    public interface IStrategyService
    {
        Task<IEnumerable<StrategyDTO>> GetAvailableStrategiesAsync();
        Task<StrategyParametersDTO?> GetStrategyParametersAsync(string strategyName);
        Task<bool> UpdateStrategyParametersAsync(string strategyName, StrategyParametersDTO parameters);
    }
}