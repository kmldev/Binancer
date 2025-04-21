using System.Collections.Generic;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Domain.Models;

namespace BinanceTradingBot.Domain.Interfaces
{
    public interface ITradingStrategy
    {
        // Define the contract for a trading strategy
        // It should take necessary inputs (e.g., market data, parameters)
        // and return a TradingSignal
        TradingSignal GenerateSignal(List<CandlestickData> candles, IConfig config);
    }
}