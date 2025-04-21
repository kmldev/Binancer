using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Interfaces;
using BinanceTradingBot.Domain.Entities;

namespace BinanceTradingBot.Infrastructure.MarketData
{
    public class MarketDataRepository : IMarketDataRepository
    {
        private readonly IBinanceApiService _binanceApiService;
        private readonly ILogger _logger;

        public MarketDataRepository(IBinanceApiService binanceApiService, ILogger logger)
        {
            _binanceApiService = binanceApiService;
            _logger = logger;
        }

        public async Task<List<BinanceTradingBot.Domain.Entities.CandlestickData>> GetCandlesticksAsync(string symbol, string interval, DateTime startTime, DateTime endTime)
        {
            _logger.LogInformation($"Fetching market data for {symbol} from repository.");
            // In a real application, this could involve caching or fetching from a database
            // For now, it delegates directly to the Binance API service
            return await _binanceApiService.GetCandlesticksAsync(symbol, interval, startTime, endTime);
        }
    }
}