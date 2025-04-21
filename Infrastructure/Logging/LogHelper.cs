using System;
using BinanceTradingBot.Domain.Interfaces;

namespace BinanceTradingBot.Infrastructure.Logging
{
    public class LogHelper : ILogger
    {
        // In a real application, this would use a proper logging framework like Serilog or NLog
        // For now, we'll just write to the console

        // Remove the constructor dependency on IConfig as logging should be independent of config details
        // public LogHelper(IConfig config)
        // {
        //     // Configuration for logging would be done here in a real scenario
        // }

        public void LogInformation(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[INFO] {DateTime.Now}: {message}");
            Console.ResetColor();
        }

        public void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {DateTime.Now}: {message}");
            Console.ResetColor();
        }

        public void LogError(string message, Exception? exception = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now}: {message}");
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception.Message}");
                Console.WriteLine(exception.StackTrace);
            }
            Console.ResetColor();
        }
    }
}