namespace BinanceTradingBot.Domain.Interfaces
{
    public interface ILogger
    {
        // Define the contract for logging
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, System.Exception? exception = null);
        // Add other logging levels as needed
    }
}