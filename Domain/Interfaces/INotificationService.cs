using System.Threading.Tasks;

namespace BinanceTradingBot.Domain.Interfaces
{
    public interface INotificationService
    {
        // Define the contract for sending notifications
        Task SendNotificationAsync(string message);
    }
}