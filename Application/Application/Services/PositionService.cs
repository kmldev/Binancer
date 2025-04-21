using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BinanceTradingBot.Application.Interfaces;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Domain.Enums;
using BinanceTradingBot.Infrastructure.Persistence.Contexts;

namespace BinanceTradingBot.Application.Services
{
    /// <summary>
    /// Service for managing trading positions
    /// </summary>
    public class PositionService : IPositionService
    {
        private readonly TradingDbContext _dbContext;
        private readonly IExchangeService _exchangeService;
        private readonly ILogger<PositionService> _logger;
        private readonly AppSettings _config;

        public PositionService(
            TradingDbContext dbContext,
            IExchangeService exchangeService,
            ILogger<PositionService> logger,
            IOptions<AppSettings> config)
        {
            _dbContext = dbContext;
            _exchangeService = exchangeService;
            _logger = logger;
            _config = config.Value;
        }

        /// <summary>
        /// Opens a new trading position
        /// </summary>
        public async Task<Position> OpenPositionAsync(string symbol, decimal entryPrice, decimal quantity, PositionType type)
        {
            try
            {
                _logger.LogInformation("Opening {Type} position for {Symbol} at {Price} with quantity {Quantity}",
                    type, symbol, entryPrice, quantity);

                // Calculate stop loss and take profit if enabled
                decimal? stopLoss = null;
                decimal? takeProfit = null;

                if (_config.UseStopLoss && _config.StopLossPercentage > 0)
                {
                    if (type == PositionType.Long)
                    {
                        stopLoss = entryPrice * (1 - (decimal)_config.StopLossPercentage);
                    }
                    else
                    {
                        stopLoss = entryPrice * (1 + (decimal)_config.StopLossPercentage);
                    }
                }

                if (_config.UseTakeProfit && _config.TakeProfitPercentage > 0)
                {
                    if (type == PositionType.Long)
                    {
                        takeProfit = entryPrice * (1 + (decimal)_config.TakeProfitPercentage);
                    }
                    else
                    {
                        takeProfit = entryPrice * (1 - (decimal)_config.TakeProfitPercentage);
                    }
                }

                // Create new position
                var position = new Position
                {
                    Symbol = symbol,
                    Type = type,
                    Status = PositionStatus.Open,
                    EntryPrice = entryPrice,
                    Quantity = quantity,
                    StopLoss = stopLoss,
                    TakeProfit = takeProfit,
                    OpenTime = DateTime.UtcNow,
                    Strategy = _config.DefaultStrategy
                };

                // Save to database
                await _dbContext.Positions.AddAsync(position);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Position {Id} opened for {Symbol}", position.Id, symbol);

                return position;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening position for {Symbol}", symbol);
                throw;
            }
        }

        /// <summary>
        /// Closes an existing position
        /// </summary>
        public async Task<Position> ClosePositionAsync(long positionId, decimal exitPrice)
        {
            try
            {
                var position = await _dbContext.Positions.FindAsync(positionId);

                if (position == null)
                {
                    throw new Exception($"Position with ID {positionId} not found");
                }

                if (position.Status == PositionStatus.Closed)
                {
                    _logger.LogWarning("Position {Id} is already closed", positionId);
                    return position;
                }

                _logger.LogInformation("Closing position {Id} for {Symbol} at {Price}",
                    positionId, position.Symbol, exitPrice);

                // Calculate profit/loss
                decimal profit;
                if (position.Type == PositionType.Long)
                {
                    profit = (exitPrice - position.EntryPrice) * position.Quantity;
                }
                else
                {
                    profit = (position.EntryPrice - exitPrice) * position.Quantity;
                }

                // Update position
                position.ExitPrice = exitPrice;
                position.CloseTime = DateTime.UtcNow;
                position.Status = PositionStatus.Closed;
                position.Profit = profit;

                // Save changes
                _dbContext.Positions.Update(position);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Position {Id} closed with profit/loss: {Profit}", positionId, profit);

                return position;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing position {Id}", positionId);
                throw;
            }
        }

        /// <summary>
        /// Gets currently open positions
        /// </summary>
        public async Task<List<Position>> GetOpenPositionsAsync()
        {
            try
            {
                return await _dbContext.Positions
                    .Where(p => p.Status == PositionStatus.Open)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving open positions");
                throw;
            }
        }

        /// <summary>
        /// Calculates current profit/loss for a position
        /// </summary>
        public async Task<decimal> CalculatePnlAsync(long positionId)
        {
            try
            {
                var position = await _dbContext.Positions.FindAsync(positionId);

                if (position == null)
                {
                    throw new Exception($"Position with ID {positionId} not found");
                }

                if (position.Status == PositionStatus.Closed && position.Profit.HasValue)
                {
                    return position.Profit.Value;
                }

                // Get current price for the symbol
                var currentPrice = await _exchangeService.GetCurrentPriceAsync(position.Symbol);

                // Calculate PnL
                return position.CalculatePnl(currentPrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating PnL for position {Id}", positionId);
                throw;
            }
        }

        /// <summary>
        /// Updates stop loss and take profit for a position
        /// </summary>
        public async Task UpdatePositionLimitsAsync(long positionId, decimal? stopLoss, decimal? takeProfit)
        {
            try
            {
                var position = await _dbContext.Positions.FindAsync(positionId);

                if (position == null)
                {
                    throw new Exception($"Position with ID {positionId} not found");
                }

                if (position.Status == PositionStatus.Closed)
                {
                    _logger.LogWarning("Cannot update limits for closed position {Id}", positionId);
                    return;
                }

                _logger.LogInformation("Updating position {Id} limits: SL={StopLoss}, TP={TakeProfit}",
                    positionId, stopLoss, takeProfit);

                position.StopLoss = stopLoss;
                position.TakeProfit = takeProfit;

                _dbContext.Positions.Update(position);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position limits for {Id}", positionId);
                throw;
            }
        }
        /// <summary>
        /// Gets closed positions for a specific date.
        /// </summary>
        public async Task<List<Position>> GetClosedPositionsForDateAsync(DateTime date)
        {
            try
            {
                _logger.LogInformation("Retrieving closed positions for date: {Date}", date.ToShortDateString());

                // Validate date
                if (date.Date > DateTime.UtcNow.Date)
                {
                    _logger.LogWarning("Cannot retrieve closed positions for a future date: {Date}", date.ToShortDateString());
                    return new List<Position>();
                }

                // Create date range for the specified day
                var startOfDay = date.Date;
                var endOfDay = date.Date.AddDays(1).AddTicks(-1);

                // Query repository for closed positions within the date range
                var closedPositions = await _dbContext.Positions
                    .Where(p => p.Status == PositionStatus.Closed &&
                                p.CloseTime >= startOfDay &&
                                p.CloseTime <= endOfDay)
                    .ToListAsync();

                // Calculate profit/loss for positions if not already set (should be set on close, but as a safeguard)
                foreach (var position in closedPositions)
                {
                    if (!position.Profit.HasValue && position.EntryPrice > 0 && position.ExitPrice.HasValue)
                    {
                        position.Profit = CalculateProfitLoss(position.EntryPrice, position.ExitPrice.Value, position.Quantity, position.Type);
                    }
                }

                _logger.LogInformation("Retrieved {Count} closed positions for date: {Date}", closedPositions.Count, date.ToShortDateString());

                return closedPositions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving closed positions for date: {Date}", date.ToShortDateString());
                throw;
            }
        }

        /// <summary>
        /// Helper method to calculate profit or loss for a position.
        /// </summary>
        private decimal CalculateProfitLoss(decimal entryPrice, decimal exitPrice, decimal quantity, PositionType type)
        {
            if (type == PositionType.Long)
            {
                return (exitPrice - entryPrice) * quantity;
            }
            else // Short position
            {
                return (entryPrice - exitPrice) * quantity;
            }
        }
    }
}