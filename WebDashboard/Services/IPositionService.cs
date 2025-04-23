using BinanceTradingBot.WebDashboard.Models;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Services
{
    public interface IPositionService
    {
        Task<IEnumerable<PositionDTO>> GetPositionsAsync(bool activeOnly = false);
        Task<PositionDTO?> GetPositionByIdAsync(long id);
        Task<ServiceResult<PositionDTO>> ClosePositionAsync(long id);
        Task<ServiceResult<PositionDTO>> UpdateStopLossTakeProfitAsync(long id, decimal? stopLoss, decimal? takeProfit);
    }
}