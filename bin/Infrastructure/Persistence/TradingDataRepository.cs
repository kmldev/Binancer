using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Interfaces;
using BinanceTradingBot.Domain.Models;
// Assuming a simple file-based persistence for now
using System.IO;
using Newtonsoft.Json;

namespace BinanceTradingBot.Infrastructure.Persistence
{
    public class TradingDataRepository : ITradingDataRepository
    {
        private const string SignalsFilePath = "trading_signals.json";
        private const string OrdersFilePath = "order_results.json";
        private readonly ILogger _logger;

        public TradingDataRepository(ILogger logger)
        {
            _logger = logger;
        }

        public async Task SaveTradingSignalAsync(TradingSignal signal)
        {
            _logger.LogInformation($"Saving trading signal: {signal.Action} at {signal.Price}");
            await AppendToJsonFileAsync(SignalsFilePath, signal);
        }

        public async Task SaveOrderResultAsync(OrderResult orderResult)
        {
            _logger.LogInformation($"Saving order result: {orderResult.Symbol} - {orderResult.Status}");
            await AppendToJsonFileAsync(OrdersFilePath, orderResult);
        }

        public async Task<List<TradingSignal>> GetTradingSignalsAsync()
        {
            _logger.LogInformation("Retrieving trading signals.");
            return await ReadFromJsonFileAsync<TradingSignal>(SignalsFilePath);
        }

        public async Task<List<OrderResult>> GetOrderResultsAsync()
        {
            _logger.LogInformation("Retrieving order results.");
            return await ReadFromJsonFileAsync<OrderResult>(OrdersFilePath);
        }

        private async Task AppendToJsonFileAsync<T>(string filePath, T data)
        {
            try
            {
                var list = new List<T>();
                if (File.Exists(filePath))
                {
                    var existingContent = await File.ReadAllTextAsync(filePath);
                    if (!string.IsNullOrWhiteSpace(existingContent))
                    {
                        list = JsonConvert.DeserializeObject<List<T>>(existingContent);
                    }
                }
                list.Add(data);
                var json = JsonConvert.SerializeObject(list, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error appending to JSON file {filePath}: {ex.Message}", ex);
            }
        }

        private async Task<List<T>> ReadFromJsonFileAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<T>();
            }
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return new List<T>();
                }
                return JsonConvert.DeserializeObject<List<T>>(content);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error reading from JSON file {filePath}: {ex.Message}", ex);
                return new List<T>();
            }
        }
    }
}