using System.Collections.Generic;

namespace BinanceTradingBot.Domain.Interfaces
{
    public interface IConfig
    {
        // Define the contract for configuration settings
        string ApiKey { get; }
        string ApiSecret { get; }
        bool UseTestnet { get; }
        int RefreshInterval { get; }
        List<TradingPair> TradingPairs { get; }
        // Add other configuration settings as needed
    }

    public class TradingPair
    {
        public string Symbol { get; set; }
        // Add other trading pair specific settings if necessary
    }
}