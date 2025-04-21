using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Models;

namespace BinanceTradingBot.Domain.Interfaces
{
    public interface ITradingDataRepository
    {
        // Define the contract for persisting trading data
        Task SaveTradingSignalAsync(TradingSignal signal);
        Task SaveOrderResultAsync(OrderResult orderResult);
        Task<List<TradingSignal>> GetTradingSignalsAsync();
        Task<List<OrderResult>> GetOrderResultsAsync();
    }
}