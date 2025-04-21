using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Domain.Models;
using BinanceTradingBot.Domain.Enums;

namespace BinanceTradingBot.Domain.Interfaces
{
    public interface IBinanceApiService
    {
        // Define the contract for interacting with the Binance API
        Task<List<CandlestickData>> GetCandlesticksAsync(string symbol, string interval, DateTime startTime, DateTime endTime);
        Task<OrderResult> PlaceOrderAsync(string symbol, OrderSide side, OrderType type, decimal quantity, decimal price);
        // Add other necessary API methods here
    }
}