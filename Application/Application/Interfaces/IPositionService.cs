using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Domain.Enums;

namespace BinanceTradingBot.Application.Interfaces
{
    /// <summary>
    /// Interface for position management services
    /// </summary>
    public interface IPositionService
    {
        /// <summary>
        /// Opens a new position
        /// </summary>
        Task<Position> OpenPositionAsync(string symbol, decimal entryPrice, decimal quantity, PositionType type);
        
        /// <summary>
        /// Closes an existing position
        /// </summary>
        Task<Position> ClosePositionAsync(long positionId, decimal exitPrice);
        
        /// <summary>
        /// Gets currently open positions
        /// </summary>
        Task<List<Position>> GetOpenPositionsAsync();
        
        /// <summary>
        /// Calculates current profit/loss for a position
        /// </summary>
        Task<decimal> CalculatePnlAsync(long positionId);
        
        /// <summary>
        /// Updates stop loss and take profit for a position
        /// </summary>
        Task UpdatePositionLimitsAsync(long positionId, decimal? stopLoss, decimal? takeProfit);
        
        /// <summary>
        /// Gets positions closed on a specific date
        /// </summary>
        Task<List<Position>> GetClosedPositionsForDateAsync(DateTime date);
    }
}