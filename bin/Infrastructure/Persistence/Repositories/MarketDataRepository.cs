using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using BinanceTradingBot.Application.Interfaces;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Infrastructure.Persistence.Contexts;

namespace BinanceTradingBot.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository pour la gestion des données de marché
    /// </summary>
    public class MarketDataRepository : IMarketDataRepository
    {
        private readonly TradingDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MarketDataRepository> _logger;
        private readonly AppSettings _config;

        /// <summary>
        /// Clé de cache pour les dernières données de bougie
        /// </summary>
        private static string GetLatestCandlesticksKey(string symbol, string interval) => $"candlesticks:{symbol}:{interval}:latest";

        /// <summary>
        /// Délai d'expiration du cache en minutes
        /// </summary>
        private readonly int _cacheExpirationMinutes;

        public MarketDataRepository(
            TradingDbContext dbContext,
            IMemoryCache cache,
            ILogger<MarketDataRepository> logger,
            IOptions<AppSettings> config)
        {
            _dbContext = dbContext;
            _cache = cache;
            _logger = logger;
            _config = config.Value;

            if (!int.TryParse(_config.CacheExpirationMinutes, out _cacheExpirationMinutes))
            {
                _cacheExpirationMinutes = 60; // Valeur par défaut
            }
        }

        /// <summary>
        /// Enregistre les données de bougie dans la base de données
        /// </summary>
        public async Task SaveCandlesticksAsync(string symbol, string interval, List<BinanceTradingBot.Domain.Entities.CandlestickData> candles)
        {
            try
            {
                var marketDataEntries = candles.Select(c => new CandlestickData
                {
                    Symbol = c.Symbol,
                    Interval = c.Interval,
                    Timestamp = c.Timestamp,
                    Open = c.Open,
                    High = c.High,
                    Low = c.Low,
                    Close = c.Close,
                    Volume = c.Volume
                }).ToList();

                // Vérifier les entrées existantes pour éviter les doublons
                foreach (var entry in marketDataEntries)
                {
                    var existingEntry = await _dbContext.Set<CandlestickData>()
                        .FirstOrDefaultAsync(m => m.Symbol == entry.Symbol &&
                                                m.Interval == entry.Interval &&
                                                m.Timestamp == entry.Timestamp);

                    if (existingEntry == null)
                    {
                        await _dbContext.Set<CandlestickData>().AddAsync(entry);
                    }
                    else
                    {
                        // Mise à jour si nécessaire (par exemple pour les bougies en cours)
                        existingEntry.Open = entry.Open;
                        existingEntry.High = entry.High;
                        existingEntry.Low = entry.Low;
                        existingEntry.Close = entry.Close;
                        existingEntry.Volume = entry.Volume;
                        _dbContext.Set<CandlestickData>().Update(existingEntry);
                    }
                }

                await _dbContext.SaveChangesAsync();

                // Mettre à jour le cache
                var cacheKey = GetLatestCandlesticksKey(symbol, interval);
                _cache.Set(cacheKey, candles, TimeSpan.FromMinutes(_cacheExpirationMinutes));

                _logger.LogInformation($"Saved {candles.Count} candlesticks for {symbol} on {interval} timeframe");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving candlesticks for {symbol} on {interval} timeframe");
                throw;
            }
        }

        /// <summary>
        /// Récupère les données de bougie depuis la base de données
        /// </summary>
        public async Task<List<BinanceTradingBot.Domain.Entities.CandlestickData>> GetCandlesticksAsync(string symbol, string interval, DateTime startTime, DateTime endTime)
        {
            try
            {
                var marketData = await _dbContext.Set<CandlestickData>()
                    .Where(m => m.Symbol == symbol &&
                               m.Interval == interval &&
                               m.Timestamp >= startTime &&
                               m.Timestamp <= endTime)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();

                return marketData.Select(m => new CandlestickData
                {
                    Symbol = m.Symbol,
                    Interval = m.Interval,
                    Timestamp = m.Timestamp,
                    Open = m.Open,
                    High = m.High,
                    Low = m.Low,
                    Close = m.Close,
                    Volume = m.Volume,
                    CloseTime = m.Timestamp.AddMinutes(GetIntervalMinutes(interval))
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving candlesticks for {symbol} from {startTime} to {endTime}");
                throw;
            }
        }

        /// <summary>
        /// Récupère les dernières données de bougie pour une paire et un intervalle
        /// </summary>
        public async Task<List<BinanceTradingBot.Domain.Entities.CandlestickData>> GetLatestCandlesticksAsync(string symbol, string interval, int limit = 100)
        {
            try
            {
                // Essayer d'abord de récupérer depuis le cache
                var cacheKey = GetLatestCandlesticksKey(symbol, interval);
                if (_cache.TryGetValue(cacheKey, out List<CandlestickData> cachedCandles))
                {
                    _logger.LogDebug($"Retrieved {cachedCandles.Count} candlesticks for {symbol} from cache");
                    return cachedCandles;
                }

                // Si pas en cache, récupérer depuis la base de données
                var marketData = await _dbContext.Set<CandlestickData>()
                    .Where(m => m.Symbol == symbol && m.Interval == interval)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(limit)
                    .ToListAsync();

                if (marketData.Count == 0)
                {
                    _logger.LogInformation($"No candlesticks found in database for {symbol} on {interval} timeframe");
                    return new List<CandlestickData>();
                }

                var candles = marketData
                    .OrderBy(m => m.Timestamp) // Remettre dans l'ordre chronologique
                    .Select(m => new CandlestickData
                    {
                        Symbol = m.Symbol,
                        Interval = m.Interval,
                        Timestamp = m.Timestamp,
                        Open = m.Open,
                        High = m.High,
                        Low = m.Low,
                        Close = m.Close,
                        Volume = m.Volume,
                        CloseTime = m.Timestamp.AddMinutes(GetIntervalMinutes(interval))
                    })
                    .ToList();

                // Stocker dans le cache
                _cache.Set(cacheKey, candles, TimeSpan.FromMinutes(_cacheExpirationMinutes));

                _logger.LogInformation($"Retrieved {candles.Count} candlesticks for {symbol} from database");

                return candles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving latest candlesticks for {symbol}");
                throw;
            }
        }

        /// <summary>
        /// Convertit une chaîne d'intervalle en minutes
        /// </summary>
        private int GetIntervalMinutes(string interval)
        {
            switch (interval.ToLower())
            {
                case "1m": return 1;
                case "3m": return 3;
                case "5m": return 5;
                case "15m": return 15;
                case "30m": return 30;
                case "1h": return 60;
                case "2h": return 120;
                case "4h": return 240;
                case "6h": return 360;
                case "8h": return 480;
                case "12h": return 720;
                case "1d": return 1440;
                case "3d": return 4320;
                case "1w": return 10080;
                case "1M": return 43200;
                default: return 1;
            }
        }
    }
}