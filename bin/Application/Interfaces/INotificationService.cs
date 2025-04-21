using System;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Models;

namespace BinanceTradingBot.Application.Interfaces
{
    /// <summary>
    /// Interface pour les services de notification
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Envoie une notification de signal de trading
        /// </summary>
        Task SendTradingSignalNotificationAsync(string symbol, TradingSignal signal);

        /// <summary>
        /// Envoie une notification d'exécution d'ordre
        /// </summary>
        Task SendOrderExecutionNotificationAsync(string symbol, OrderResult order);

        /// <summary>
        /// Envoie une notification d'erreur
        /// </summary>
        Task SendErrorNotificationAsync(string errorMessage, Exception? exception = null);
    }
}