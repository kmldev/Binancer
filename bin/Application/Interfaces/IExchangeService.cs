using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Domain.Enums;
using BinanceTradingBot.Domain.Models;

namespace BinanceTradingBot.Application.Interfaces
{
    /// <summary>
    /// Interface pour les services d'échange avec Binance
    /// </summary>
    public interface IExchangeService
    {
        /// <summary>
        /// Récupère les données de marché récentes pour une paire et un intervalle spécifiques
        /// </summary>
        Task<List<CandlestickData>> GetCandlesticksAsync(string symbol, string interval, int limit = 100);

        /// <summary>
        /// Récupère le solde actuel d'un actif
        /// </summary>
        Task<decimal> GetBalanceAsync(string asset);

        /// <summary>
        /// Récupère le prix actuel d'une paire de trading
        /// </summary>
        Task<decimal> GetCurrentPriceAsync(string symbol);

        /// <summary>
        /// Place un ordre d'achat ou de vente sur le marché
        /// </summary>
        Task<OrderResult> PlaceOrderAsync(string symbol, OrderType type, OrderSide side, decimal quantity, decimal? price = null);

        /// <summary>
        /// Annule un ordre existant
        /// </summary>
        Task<bool> CancelOrderAsync(string symbol, long orderId);

        /// <summary>
        /// Vérifie le statut d'un ordre
        /// </summary>
        Task<OrderResult> CheckOrderStatusAsync(string symbol, long orderId);

        /// <summary>
        /// Récupère l'historique des ordres pour une paire spécifique
        /// </summary>
        Task<List<OrderResult>> GetOrderHistoryAsync(string symbol, int limit = 50);
    }
}