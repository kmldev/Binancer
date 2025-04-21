using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Models;
using BinanceTradingBot.Domain.Enums;

namespace BinanceTradingBot.Application.Interfaces
{
    /// <summary>
    /// Interface pour les services de stratégie de trading
    /// </summary>
    public interface IStrategyService
    {
        /// <summary>
        /// Génère un signal de trading basé sur l'analyse de marché
        /// </summary>
        Task<TradingSignal> GenerateSignalAsync(string symbol, string interval);

        /// <summary>
        /// Évalue la performance d'une stratégie sur des données historiques
        /// </summary>
        Task<StrategyPerformance> BacktestStrategyAsync(string symbol, string interval, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Configure les paramètres de la stratégie
        /// </summary>
        void ConfigureStrategy(Dictionary<string, object> parameters);
    }
}