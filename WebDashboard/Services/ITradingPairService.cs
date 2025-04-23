using BinanceTradingBot.WebDashboard.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Services
{
    public interface ITradingPairService
    {
        Task<IEnumerable<TradingPairDTO>> GetAllTradingPairsAsync();
        Task<TradingPairDTO?> GetTradingPairBySymbolAsync(string symbol);
        Task<TradingPairDTO> CreateTradingPairAsync(TradingPairDTO pairDTO);
        Task<bool> UpdateTradingPairAsync(TradingPairDTO pairDTO);
        Task<bool> ToggleTradingPairActiveAsync(string symbol);
    }
}