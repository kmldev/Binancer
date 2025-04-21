using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BinanceTradingBot.Domain.Interfaces;
using BinanceTradingBot.Domain.Models; // To use TradingPair

namespace BinanceTradingBot.Infrastructure.Configuration
{
    // This class is responsible for loading configuration from a file.
    // In a real application, you might use a library like Microsoft.Extensions.Configuration.
    public static class ConfigHelper
    {
        private const string ConfigFilePath = "appsettings.json"; // Or your preferred config file name

        public static async Task<IConfig> LoadConfigAsync()
        {
            if (!File.Exists(ConfigFilePath))
            {
                // Create a default config file if it doesn't exist
                var defaultConfig = new AppConfig // Assuming AppConfig is a concrete implementation of IConfig
                {
                    ApiKey = "YOUR_API_KEY",
                    ApiSecret = "YOUR_API_SECRET",
                    UseTestnet = true,
                    RefreshInterval = 60, // seconds
                    TradingPairs = new List<TradingPair>
                    {
                        new TradingPair { Symbol = "BTCUSDT" }
                    }
                    // Add other default settings
                };
                var json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                await File.WriteAllTextAsync(ConfigFilePath, json);
                // In a real app, you might throw an exception or log a warning here
                // For now, we'll return the default config
                return defaultConfig;
            }

            try
            {
                var json = await File.ReadAllTextAsync(ConfigFilePath);
                // Assuming AppConfig is a concrete implementation of IConfig
                var config = JsonConvert.DeserializeObject<AppConfig>(json);
                return config;
            }
            catch (System.Exception ex)
            {
                // Log the error and potentially return a default config or throw
                // For now, we'll just throw
                throw new System.Exception($"Error loading configuration from {ConfigFilePath}", ex);
            }
        }
    }

    // Concrete implementation of IConfig (can be in the same file or a separate one)
    public class AppConfig : IConfig
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public bool UseTestnet { get; set; }
        public int RefreshInterval { get; set; }
        public List<TradingPair> TradingPairs { get; set; }
        // Implement other config properties
    }
}