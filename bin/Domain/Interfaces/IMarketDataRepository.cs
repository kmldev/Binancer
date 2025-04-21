using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Entities;

namespace BinanceTradingBot.Domain.Interfaces
{
    public interface IMarketDataRepository
    {
        // Define the contract for fetching market data
        Task<List<CandlestickData>> GetCandlesticksAsync(string symbol, string interval, DateTime startTime, DateTime endTime);
    }
}