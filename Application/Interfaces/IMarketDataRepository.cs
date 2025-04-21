using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Entities;

namespace BinanceTradingBot.Application.Interfaces
{
    /// <summary>
    /// Interface pour les dépôts de données de marché
    /// </summary>
    public interface IMarketDataRepository
    {
        /// <summary>
        /// Enregistre les données de bougie dans la base de données
        /// </summary>
        Task SaveCandlesticksAsync(string symbol, string interval, List<CandlestickData> candles);

        /// <summary>
        /// Récupère les données de bougie depuis la base de données
        /// </summary>
        Task<List<CandlestickData>> GetCandlesticksAsync(string symbol, string interval, DateTime startTime, DateTime endTime);

        /// <summary>
        /// Récupère les dernières données de bougie pour une paire et un intervalle
        /// </summary>
        Task<List<CandlestickData>> GetLatestCandlesticksAsync(string symbol, string interval, int limit = 100);
    }
}