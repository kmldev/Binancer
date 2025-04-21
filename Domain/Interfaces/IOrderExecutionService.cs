using System.Threading.Tasks;
using BinanceTradingBot.Domain.Models;

namespace BinanceTradingBot.Domain.Interfaces
{
    public interface IOrderExecutionService
    {
        // Define the contract for executing orders
        Task<OrderResult> ExecuteSignalAsync(string symbol, TradingSignal signal);
    }
}