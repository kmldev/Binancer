
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BinanceTradingBot.Models;
using BinanceTradingBot.Services;
using BinanceTradingBot.Repositories;
using BinanceTradingBot.Utilities;

namespace BinanceTradingBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = await ConfigHelper.LoadConfigAsync();
            var logger = new LogHelper(config);
            var apiService = new BinanceApiService(config.ApiKey, config.ApiSecret, logger, config.UseTestnet);
            var marketDataRepository = new MarketDataRepository(logger);
            var strategyService = new TradingStrategyService(apiService, marketDataRepository, logger, config);
            var orderExecutionService = new OrderExecutionService(apiService, logger, config);

            Console.WriteLine("Running Binance Trading Bot...");

            while (true)
            {
                foreach (var pair in config.TradingPairs)
                {
                    var signal = await strategyService.GenerateSignalAsync(pair.Symbol, "15m");

                    if (signal.Action != SignalAction.None)
                    {
                        var result = await orderExecutionService.ExecuteSignalAsync(pair.Symbol, signal);
                        Console.WriteLine($"{signal.Action} {pair.Symbol} at {signal.Price} => Result: {result.Status}");
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(config.RefreshInterval));
            }
        }
    }
}
