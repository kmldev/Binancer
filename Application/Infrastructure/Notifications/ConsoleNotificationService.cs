using System;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Interfaces;

namespace BinanceTradingBot.Infrastructure.Notifications
{
    public class ConsoleNotificationService : INotificationService
    {
        private readonly ILogger _logger;

        public ConsoleNotificationService(ILogger logger)
        {
            _logger = logger;
        }

        public Task SendNotificationAsync(string message)
        {
            _logger.LogInformation($"NOTIFICATION: {message}");
            // In a real application, this would send notifications via email, SMS, Telegram, etc.
            return Task.CompletedTask;
        }
    }
}