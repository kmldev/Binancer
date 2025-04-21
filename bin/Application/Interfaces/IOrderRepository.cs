using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Models;

namespace BinanceTradingBot.Application.Interfaces
{
    /// <summary>
    /// Interface pour les dépôts d'ordres
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Enregistre un nouvel ordre dans la base de données
        /// </summary>
        Task<long> SaveOrderAsync(OrderResult order);

        /// <summary>
        /// Met à jour un ordre existant dans la base de données
        /// </summary>
        Task UpdateOrderAsync(OrderResult order);

        /// <summary>
        /// Récupère un ordre par son ID
        /// </summary>
        Task<OrderResult> GetOrderByIdAsync(long orderId);

        /// <summary>
        /// Récupère tous les ordres pour une paire spécifique
        /// </summary>
        Task<List<OrderResult>> GetOrdersBySymbolAsync(string symbol, int limit = 50);

        /// <summary>
        /// Récupère les ordres ouverts
        /// </summary>
        Task<List<OrderResult>> GetOpenOrdersAsync();
    }
}