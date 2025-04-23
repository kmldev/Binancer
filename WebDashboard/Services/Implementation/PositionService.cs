using Microsoft.EntityFrameworkCore;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Domain.Enums;
using BinanceTradingBot.Infrastructure.Persistence.Contexts;
using BinanceTradingBot.WebDashboard.Models;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using BinanceTradingBot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BinanceTradingBot.WebDashboard.Services.Implementation
{
    public class PositionService : IPositionService
    {
        private readonly TradingDbContext _dbContext;
        private readonly ILogger<PositionService> _logger;
        private readonly IExchangeService _exchangeService;

        public PositionService(
            TradingDbContext dbContext,
            ILogger<PositionService> logger,
            IExchangeService exchangeService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _exchangeService = exchangeService;
        }

        public async Task<IEnumerable<PositionDTO>> GetPositionsAsync(bool activeOnly = false)
        {
            try
            {
                var query = _dbContext.Positions.AsQueryable();

                if (activeOnly)
                {
                    query = query.Where(p => p.Status == PositionStatus.Open);
                }

                var positions = await query
                    .OrderByDescending(p => p.Status == PositionStatus.Open ? 1 : 0)
                    .ThenByDescending(p => p.OpenTime)
                    .ToListAsync();

                return positions.Select(MapToPositionDTO).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des positions");
                throw;
            }
        }

        public async Task<PositionDTO?> GetPositionByIdAsync(long id)
        {
            try
            {
                var position = await _dbContext.Positions
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (position == null)
                    return null;

                return MapToPositionDTO(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la position {PositionId}", id);
                throw;
            }
        }

        public async Task<ServiceResult<PositionDTO>> ClosePositionAsync(long id)
        {
            try
            {
                var position = await _dbContext.Positions
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (position == null)
                {
                    return ServiceResult<PositionDTO>.Error($"Position {id} non trouvée", notFound: true);
                }

                if (position.Status == PositionStatus.Closed)
                {
                    return ServiceResult<PositionDTO>.Error($"La position {id} est déjà fermée");
                }

                // Dans une implémentation réelle, il faudrait appeler l'API d'échange pour fermer la position
                decimal currentPrice = await GetCurrentPriceAsync(position.Symbol);

                if (currentPrice <= 0)
                {
                    return ServiceResult<PositionDTO>.Error($"Impossible d'obtenir le prix actuel pour {position.Symbol}");
                }

                // Calculer le profit
                decimal profit = position.CalculatePnl(currentPrice);

                // Mettre à jour la position
                position.Status = PositionStatus.Closed;
                position.CloseTime = DateTime.UtcNow;
                position.ExitPrice = currentPrice;
                position.Profit = profit;

                await _dbContext.SaveChangesAsync();

                var positionDTO = MapToPositionDTO(position);
                return ServiceResult<PositionDTO>.Ok(positionDTO, "Position fermée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la fermeture de la position {PositionId}", id);
                return ServiceResult<PositionDTO>.Error($"Erreur lors de la fermeture de la position: {ex.Message}");
            }
        }

        public async Task<ServiceResult<PositionDTO>> UpdateStopLossTakeProfitAsync(long id, decimal? stopLoss, decimal? takeProfit)
        {
            try
            {
                var position = await _dbContext.Positions
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (position == null)
                {
                    return ServiceResult<PositionDTO>.Error($"Position {id} non trouvée", notFound: true);
                }

                if (position.Status == PositionStatus.Closed)
                {
                    return ServiceResult<PositionDTO>.Error($"La position {id} est fermée et ne peut pas être modifiée");
                }

                // Valider les valeurs de SL/TP par rapport au prix d'entrée
                decimal currentPrice = await GetCurrentPriceAsync(position.Symbol);

                if (stopLoss.HasValue)
                {
                    if (position.Type == PositionType.Long && stopLoss.Value >= currentPrice)
                    {
                        return ServiceResult<PositionDTO>.Error("Le stop loss pour une position longue doit être inférieur au prix actuel");
                    }
                    else if (position.Type == PositionType.Short && stopLoss.Value <= currentPrice)
                    {
                        return ServiceResult<PositionDTO>.Error("Le stop loss pour une position courte doit être supérieur au prix actuel");
                    }
                }

                if (takeProfit.HasValue)
                {
                    if (position.Type == PositionType.Long && takeProfit.Value <= currentPrice)
                    {
                        return ServiceResult<PositionDTO>.Error("Le take profit pour une position longue doit être supérieur au prix actuel");
                    }
                    else if (position.Type == PositionType.Short && takeProfit.Value >= currentPrice)
                    {
                        return ServiceResult<PositionDTO>.Error("Le take profit pour une position courte doit être inférieur au prix actuel");
                    }
                }

                // Mettre à jour la position
                position.StopLoss = stopLoss;
                position.TakeProfit = takeProfit;

                await _dbContext.SaveChangesAsync();

                var positionDTO = MapToPositionDTO(position);
                return ServiceResult<PositionDTO>.Ok(positionDTO, "Stop Loss et Take Profit mis à jour avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour SL/TP de la position {PositionId}", id);
                return ServiceResult<PositionDTO>.Error($"Erreur lors de la mise à jour: {ex.Message}");
            }
        }

        private PositionDTO MapToPositionDTO(Position position)
        {
            decimal currentProfit = 0;
            decimal currentProfitPercentage = 0;

            if (position.Status == PositionStatus.Open)
            {
                var currentPrice = GetCurrentPriceAsync(position.Symbol).Result;
                currentProfit = position.CalculatePnl(currentPrice);
                currentProfitPercentage = position.EntryPrice > 0
                    ? (currentProfit / (position.EntryPrice * position.Quantity)) * 100
                    : 0;
            }
            else if (position.Status == PositionStatus.Closed && position.Profit.HasValue)
            {
                currentProfit = position.Profit.Value;
                currentProfitPercentage = position.EntryPrice > 0
                    ? (position.Profit.Value / (position.EntryPrice * position.Quantity)) * 100
                    : 0;
            }

            return new PositionDTO
            {
                Id = position.Id,
                Symbol = position.Symbol,
                Type = position.Type,
                Status = position.Status,
                EntryPrice = position.EntryPrice,
                ExitPrice = position.ExitPrice,
                Quantity = position.Quantity,
                StopLoss = position.StopLoss,
                TakeProfit = position.TakeProfit,
                OpenTime = position.OpenTime,
                CloseTime = position.CloseTime,
                Profit = position.Profit,
                Strategy = position.Strategy,
                CurrentProfit = currentProfit,
                CurrentProfitPercentage = currentProfitPercentage
            };
        }

        private async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            try
            {
                // Dans une implémentation réelle, utiliser l'API d'échange
                return await _exchangeService.GetCurrentPriceAsync(symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du prix actuel pour {Symbol}", symbol);

                // Valeurs de test en cas d'erreur
                var mockPrices = new Dictionary<string, decimal>
                {
                    { "BTCUSDT", 60000 },
                    { "ETHUSDT", 3000 },
                    { "BNBUSDT", 500 },
                    { "SOLUSDT", 100 }
                };

                if (mockPrices.TryGetValue(symbol, out var price))
                    return price;

                return 0;
            }
        }
    }
}