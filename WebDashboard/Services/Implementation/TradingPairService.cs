using Microsoft.EntityFrameworkCore;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Infrastructure.Persistence.Contexts;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BinanceTradingBot.WebDashboard.Services.Implementation
{
    public class TradingPairService : ITradingPairService
    {
        private readonly TradingDbContext _dbContext;
        private readonly ILogger<TradingPairService> _logger;
        private readonly IMemoryCache _cache;
        private const string CacheKeyPrefix = "TradingPair_";

        public TradingPairService(TradingDbContext dbContext, ILogger<TradingPairService> logger, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _logger = logger;
            _cache = cache;
        }

        public async Task<IEnumerable<TradingPairDTO>> GetAllTradingPairsAsync()
        {
            try
            {
                const string cacheKey = "AllTradingPairs";

                // Tentative de récupération depuis le cache
                if (_cache.TryGetValue(cacheKey, out IEnumerable<TradingPairDTO>? cachedPairs))
                {
                    return cachedPairs!;
                }

                // Récupération depuis la base de données
                var pairs = await _dbContext.TradingPairs
                    .AsNoTracking()
                    .ToListAsync();

                var pairDTOs = pairs.Select(MapToTradingPairDTO).ToList();

                // Mise en cache pour 5 minutes
                _cache.Set(cacheKey, pairDTOs, TimeSpan.FromMinutes(5));

                return pairDTOs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de toutes les paires de trading");
                throw;
            }
        }

        public async Task<TradingPairDTO?> GetTradingPairBySymbolAsync(string symbol)
        {
            try
            {
                string cacheKey = $"{CacheKeyPrefix}{symbol}";

                // Tentative de récupération depuis le cache
                if (_cache.TryGetValue(cacheKey, out TradingPairDTO? cachedPair))
                {
                    return cachedPair;
                }

                // Récupération depuis la base de données
                var pair = await _dbContext.TradingPairs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Symbol == symbol);

                if (pair == null)
                    return null;

                var pairDTO = MapToTradingPairDTO(pair);

                // Mise en cache pour 5 minutes
                _cache.Set(cacheKey, pairDTO, TimeSpan.FromMinutes(5));

                return pairDTO;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la paire de trading {Symbol}", symbol);
                throw;
            }
        }

        public async Task<TradingPairDTO> CreateTradingPairAsync(TradingPairDTO pairDTO)
        {
            try
            {
                // Vérifier si la paire existe déjà
                var existingPair = await _dbContext.TradingPairs
                    .FirstOrDefaultAsync(p => p.Symbol == pairDTO.Symbol);

                if (existingPair != null)
                {
                    throw new InvalidOperationException($"Une paire de trading avec le symbole {pairDTO.Symbol} existe déjà");
                }

                // Créer une nouvelle paire
                var newPair = new TradingPair
                {
                    Symbol = pairDTO.Symbol,
                    BaseAsset = pairDTO.BaseAsset,
                    QuoteAsset = pairDTO.QuoteAsset,
                    PricePrecision = pairDTO.PricePrecision,
                    QuantityPrecision = pairDTO.QuantityPrecision,
                    MinNotional = pairDTO.MinNotional,
                    MinQuantity = pairDTO.MinQuantity,
                    MaxQuantity = pairDTO.MaxQuantity,
                    StepSize = pairDTO.StepSize,
                    TickSize = pairDTO.TickSize,
                    IsActive = pairDTO.IsActive
                };

                _dbContext.TradingPairs.Add(newPair);
                await _dbContext.SaveChangesAsync();

                // Invalider le cache
                InvalidateCache("AllTradingPairs");

                return MapToTradingPairDTO(newPair);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création d'une paire de trading {Symbol}", pairDTO.Symbol);
                throw;
            }
        }

        public async Task<bool> UpdateTradingPairAsync(TradingPairDTO pairDTO)
        {
            try
            {
                var existingPair = await _dbContext.TradingPairs
                    .FirstOrDefaultAsync(p => p.Symbol == pairDTO.Symbol);

                if (existingPair == null)
                {
                    return false;
                }

                // Mettre à jour les propriétés
                existingPair.BaseAsset = pairDTO.BaseAsset;
                existingPair.QuoteAsset = pairDTO.QuoteAsset;
                existingPair.PricePrecision = pairDTO.PricePrecision;
                existingPair.QuantityPrecision = pairDTO.QuantityPrecision;
                existingPair.MinNotional = pairDTO.MinNotional;
                existingPair.MinQuantity = pairDTO.MinQuantity;
                existingPair.MaxQuantity = pairDTO.MaxQuantity;
                existingPair.StepSize = pairDTO.StepSize;
                existingPair.TickSize = pairDTO.TickSize;
                existingPair.IsActive = pairDTO.IsActive;

                await _dbContext.SaveChangesAsync();

                // Invalider le cache
                InvalidateCache("AllTradingPairs");
                InvalidateCache($"{CacheKeyPrefix}{pairDTO.Symbol}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la paire de trading {Symbol}", pairDTO.Symbol);
                throw;
            }
        }

        public async Task<bool> ToggleTradingPairActiveAsync(string symbol)
        {
            try
            {
                var existingPair = await _dbContext.TradingPairs
                    .FirstOrDefaultAsync(p => p.Symbol == symbol);

                if (existingPair == null)
                {
                    return false;
                }

                // Inverser l'état actif
                existingPair.IsActive = !existingPair.IsActive;

                await _dbContext.SaveChangesAsync();

                // Invalider le cache
                InvalidateCache("AllTradingPairs");
                InvalidateCache($"{CacheKeyPrefix}{symbol}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du basculement de l'état actif de la paire {Symbol}", symbol);
                throw;
            }
        }

        private TradingPairDTO MapToTradingPairDTO(TradingPair pair)
        {
            return new TradingPairDTO
            {
                Symbol = pair.Symbol,
                BaseAsset = pair.BaseAsset,
                QuoteAsset = pair.QuoteAsset,
                PricePrecision = pair.PricePrecision,
                QuantityPrecision = pair.QuantityPrecision,
                MinNotional = pair.MinNotional,
                MinQuantity = pair.MinQuantity,
                MaxQuantity = pair.MaxQuantity,
                StepSize = pair.StepSize,
                TickSize = pair.TickSize,
                IsActive = pair.IsActive
            };
        }

        private void InvalidateCache(string key)
        {
            _cache.Remove(key);
        }
    }
}