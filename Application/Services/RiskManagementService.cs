using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BinanceTradingBot.Application.Interfaces;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Domain.Enums;

namespace BinanceTradingBot.Application.Services
{
    /// <summary>
    /// Service for managing trading risk and portfolio exposure
    /// </summary>
    public class RiskManagementService
    {
        private readonly IExchangeService _exchangeService;
        private readonly IPositionService _positionService;
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<RiskManagementService> _logger;
        private readonly AppSettings _config;

        public RiskManagementService(
            IExchangeService exchangeService,
            IPositionService positionService,
            IMarketDataRepository marketDataRepository,
            INotificationService notificationService,
            ILogger<RiskManagementService> logger,
            IOptions<AppSettings> config)
        {
            _exchangeService = exchangeService;
            _positionService = positionService;
            _marketDataRepository = marketDataRepository;
            _notificationService = notificationService;
            _logger = logger;
            _config = config.Value;
        }

        /// <summary>
        /// Validates whether a new position can be opened based on risk parameters
        /// </summary>
        public async Task<(bool IsAllowed, string Reason)> ValidateNewPositionAsync(string symbol, decimal price, decimal quantity)
        {
            try
            {
                // Check portfolio exposure
                var exposure = await CalculatePortfolioExposureAsync();
                decimal maxExposure = _config.MaxPortfolioExposure > 0 ? _config.MaxPortfolioExposure : 0.8m;

                if (exposure >= maxExposure)
                {
                    return (false, $"Portfolio exposure ({exposure:P2}) exceeds maximum ({maxExposure:P2})");
                }

                // Check position size
                decimal totalBalance = await GetTotalBalanceInUsdtAsync();
                decimal positionSize = price * quantity;
                decimal positionPercentage = totalBalance > 0 ? positionSize / totalBalance : 0;
                decimal maxPositionSize = _config.MaxPositionSize > 0 ? _config.MaxPositionSize : 0.2m;

                if (positionPercentage > maxPositionSize)
                {
                    return (false, $"Position size ({positionPercentage:P2}) exceeds maximum ({maxPositionSize:P2})");
                }

                // Check symbol volatility
                var volatility = await CalculateSymbolVolatilityAsync(symbol);
                decimal maxVolatility = _config.MaxAllowedVolatility > 0 ? _config.MaxAllowedVolatility : 0.05m;

                if (volatility > maxVolatility)
                {
                    return (false, $"Symbol volatility ({volatility:P2}) exceeds maximum ({maxVolatility:P2})");
                }

                // Check correlation with existing positions
                var openPositions = await _positionService.GetOpenPositionsAsync();
                if (openPositions.Any(p => p.Symbol == symbol) && !_config.AllowMultiplePositions)
                {
                    return (false, "Position already exists for this symbol and multiple positions are not allowed");
                }

                // Check trading session
                if (!IsTradingSessionActive())
                {
                    return (false, "Outside of allowed trading hours");
                }

                // Check daily loss limit
                var dailyPnl = await CalculateDailyPnLAsync();
                if (dailyPnl < -_config.MaxDailyLoss)
                {
                    return (false, $"Daily loss limit reached (${dailyPnl:F2})");
                }

                return (true, "Position allowed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating new position for {Symbol}", symbol);
                return (false, $"Error validating position: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates current portfolio exposure (percent of capital in positions)
        /// </summary>
        public async Task<decimal> CalculatePortfolioExposureAsync()
        {
            try
            {
                decimal totalBalance = await GetTotalBalanceInUsdtAsync();

                if (totalBalance <= 0)
                {
                    return 0;
                }

                decimal positionValue = 0;
                var openPositions = await _positionService.GetOpenPositionsAsync();

                foreach (var position in openPositions)
                {
                    var currentPrice = await _exchangeService.GetCurrentPriceAsync(position.Symbol);
                    positionValue += position.Quantity * currentPrice;
                }

                return positionValue / totalBalance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating portfolio exposure");
                throw;
            }
        }

        /// <summary>
        /// Gets total account balance converted to USDT
        /// </summary>
        public async Task<decimal> GetTotalBalanceInUsdtAsync()
        {
            try
            {
                decimal totalUsdtBalance = 0;

                // Get USDT balance directly
                totalUsdtBalance += await _exchangeService.GetBalanceAsync("USDT");

                // For other major assets, convert to USDT
                string[] majorAssets = { "BTC", "ETH", "BNB" };

                foreach (var asset in majorAssets)
                {
                    var balance = await _exchangeService.GetBalanceAsync(asset);

                    if (balance > 0)
                    {
                        // Get price in USDT
                        var assetPrice = await _exchangeService.GetCurrentPriceAsync($"{asset}USDT");
                        totalUsdtBalance += balance * assetPrice;
                    }
                }

                return totalUsdtBalance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total balance in USDT");
                throw;
            }
        }

        /// <summary>
        /// Calculates historical volatility for a symbol
        /// </summary>
        public async Task<decimal> CalculateSymbolVolatilityAsync(string symbol, string interval = "1d", int period = 14)
        {
            try
            {
                // Get historical data
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-period * 2); // Get more data than needed for calculations

                var candles = await _marketDataRepository.GetCandlesticksAsync(symbol, interval, startDate, endDate);

                if (candles.Count < period)
                {
                    _logger.LogWarning("Not enough data to calculate volatility for {Symbol}", symbol);
                    return 0;
                }

                // Take the most recent candles
                var recentCandles = candles.OrderByDescending(c => c.OpenTime).Take(period).OrderBy(c => c.OpenTime).ToList();
                var closePrices = recentCandles.Select(c => c.Close).ToList();

                // Calculate log returns
                var returns = new List<decimal>();
                for (int i = 1; i < closePrices.Count; i++)
                {
                    returns.Add((decimal)Math.Log((double)(closePrices[i] / closePrices[i - 1])));
                }

                // Calculate standard deviation of returns
                var mean = returns.Average();
                var sumSquaredDiffs = returns.Sum(r => (r - mean) * (r - mean));
                var variance = sumSquaredDiffs / (returns.Count - 1);
                var stdDev = (decimal)Math.Sqrt((double)variance);

                // Annualize volatility (based on the interval)
                var annualizationFactor = GetAnnualizationFactor(interval);
                var annualizedVolatility = stdDev * (decimal)Math.Sqrt(annualizationFactor);

                return annualizedVolatility;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating volatility for {Symbol}", symbol);
                return 0;
            }
        }

        /// <summary>
        /// Calculates daily profit/loss
        /// </summary>
        public async Task<decimal> CalculateDailyPnLAsync()
        {
            try
            {
                // Get today's completed trades
                var today = DateTime.UtcNow.Date;
                var closedPositions = await _positionService.GetClosedPositionsForDateAsync(today);

                // Sum up the profit/loss
                return closedPositions.Sum(p => p.Profit ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating daily PnL");
                return 0;
            }
        }

        /// <summary>
        /// Checks if current time is within allowed trading hours
        /// </summary>
        public bool IsTradingSessionActive()
        {
            // If no trading hours are configured, allow trading 24/7
            if (!_config.RestrictTradingHours)
            {
                return true;
            }

            var currentTime = DateTime.UtcNow.TimeOfDay;
            var startTime = TimeSpan.Parse(_config.TradingHoursStart ?? "00:00:00");
            var endTime = TimeSpan.Parse(_config.TradingHoursEnd ?? "23:59:59");

            // Handle overnight sessions
            if (startTime > endTime)
            {
                return currentTime >= startTime || currentTime <= endTime;
            }

            return currentTime >= startTime && currentTime <= endTime;
        }

        /// <summary>
        /// Adjusts position size based on volatility and risk parameters
        /// </summary>
        public async Task<decimal> CalculatePositionSizeAsync(string symbol, decimal price, decimal availableCapital)
        {
            try
            {
                // Calculate base position size based on risk percentage
                decimal riskPercentage = (decimal)_config.RiskPerTradePercentage;
                decimal basePositionValue = availableCapital * riskPercentage;

                // Adjust for volatility
                var volatility = await CalculateSymbolVolatilityAsync(symbol);
                decimal volatilityFactor = 0.05m; // Base volatility reference (5%)
                decimal volatilityAdjustment = volatilityFactor / Math.Max(volatility, 0.01m);

                // Cap the adjustment to avoid extremely large or small positions
                volatilityAdjustment = Math.Min(Math.Max(volatilityAdjustment, 0.5m), 2.0m);

                decimal adjustedPositionValue = basePositionValue * volatilityAdjustment;
                decimal quantity = adjustedPositionValue / price;

                // Round according to the symbol's quantity precision
                var tradingPair = _config.TradingPairs.FirstOrDefault(p => p.Symbol == symbol);

                if (tradingPair != null)
                {
                    quantity = Math.Floor(quantity * (decimal)Math.Pow(10, tradingPair.QuantityPrecision)) /
                                  (decimal)Math.Pow(10, tradingPair.QuantityPrecision);

                    // Ensure minimum quantity
                    if (quantity < tradingPair.MinQuantity)
                    {
                        quantity = tradingPair.MinQuantity;
                    }

                    // Ensure maximum quantity
                    if (quantity > tradingPair.MaxQuantity)
                    {
                        quantity = tradingPair.MaxQuantity;
                    }
                }

                return quantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating position size for {Symbol}", symbol);
                throw;
            }
        }

        /// <summary>
        /// Gets annualization factor for volatility calculation based on interval
        /// </summary>
        private double GetAnnualizationFactor(string interval)
        {
            // Approximate number of intervals in a year
            switch (interval.ToLower())
            {
                case "1m": return 365 * 24 * 60;    // minutes in a year
                case "5m": return 365 * 24 * 12;    // 5-minute intervals in a year
                case "15m": return 365 * 24 * 4;    // 15-minute intervals in a year
                case "30m": return 365 * 24 * 2;    // 30-minute intervals in a year
                case "1h": return 365 * 24;         // hours in a year
                case "4h": return 365 * 6;          // 4-hour intervals in a year
                case "1d": return 365;              // days in a year
                case "1w": return 52;               // weeks in a year
                case "1M": return 12;               // months in a year
                default: return 365;                // default to daily
            }
        }

        /// <summary>
        /// Monitors current positions to detect adverse conditions and take protective actions
        /// </summary>
        public async Task MonitorPositionsAsync()
        {
            try
            {
                _logger.LogInformation("Running position monitoring check");

                var openPositions = await _positionService.GetOpenPositionsAsync();

                if (openPositions.Count == 0)
                {
                    _logger.LogDebug("No open positions to monitor");
                    return;
                }

                // Check overall market conditions
                bool marketVolatile = await IsMarketVolatileAsync();
                bool marketTrending = await IsMarketTrendingAsync();

                foreach (var position in openPositions)
                {
                    try
                    {
                        // Get current price and calculate P&L
                        var currentPrice = await _exchangeService.GetCurrentPriceAsync(position.Symbol);
                        var pnl = position.CalculatePnl(currentPrice);
                        var pnlPercentage = position.EntryPrice > 0 ? pnl / (position.EntryPrice * position.Quantity) : 0;

                        _logger.LogDebug("Position {Id} for {Symbol}: Entry={Entry}, Current={Current}, P&L={PnL:P2}",
                            position.Id, position.Symbol, position.EntryPrice, currentPrice, pnlPercentage);

                        // Check for emergency exit conditions
                        if (ShouldEmergencyExit(position, currentPrice, pnlPercentage, marketVolatile))
                        {
                            _logger.LogWarning("Emergency exit triggered for position {Id} on {Symbol}",
                                position.Id, position.Symbol);

                            await EmergencyExitPositionAsync(position);
                            continue;
                        }

                        // Check for dynamic stop loss adjustment
                        if (_config.UseDynamicStopLoss && position.StopLoss.HasValue)
                        {
                            var newStopLoss = CalculateDynamicStopLoss(position, currentPrice, pnlPercentage);

                            if (newStopLoss != position.StopLoss)
                            {
                                await _positionService.UpdatePositionLimitsAsync(
                                    position.Id,
                                    newStopLoss,
                                    position.TakeProfit);

                                _logger.LogInformation("Updated stop loss for position {Id} on {Symbol} from {OldStop} to {NewStop}",
                                    position.Id, position.Symbol, position.StopLoss, newStopLoss);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error monitoring position {Id} for {Symbol}", position.Id, position.Symbol);
                    }
                }

                // Check overall portfolio exposure
                await CheckPortfolioExposureAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during position monitoring");
            }
        }

        /// <summary>
        /// Determines if emergency exit should be triggered for a position
        /// </summary>
        private bool ShouldEmergencyExit(Position position, decimal currentPrice, decimal pnlPercentage, bool marketVolatile)
        {
            // Emergency exit on extreme loss
            if (pnlPercentage < -_config.EmergencyExitThreshold)
            {
                _logger.LogWarning("Emergency exit: Extreme loss detected for {Symbol}: {PnL:P2}",
                    position.Symbol, pnlPercentage);
                return true;
            }

            // Emergency exit on extreme volatility if in profit
            if (marketVolatile && pnlPercentage > 0.01m)
            {
                _logger.LogWarning("Emergency exit: Market volatility detected while in profit for {Symbol}",
                    position.Symbol);
                return true;
            }

            // Emergency exit on time-based criteria
            var positionAge = DateTime.UtcNow - position.OpenTime;

            if (positionAge.TotalDays > _config.MaxPositionDays && pnlPercentage < 0)
            {
                _logger.LogWarning("Emergency exit: Position age {Days} days exceeds maximum {Max} days with negative P&L",
                    positionAge.TotalDays, _config.MaxPositionDays);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Executes emergency exit for a position with market order
        /// </summary>
        private async Task EmergencyExitPositionAsync(Position position)
        {
            try
            {
                // Place market sell order
                var orderSide = OrderSide.Sell;
                var result = await _exchangeService.PlaceOrderAsync(
                    position.Symbol,
                    OrderType.Market,
                    orderSide,
                    position.Quantity);

                if (result.Status == OrderStatus.Filled || result.Status == OrderStatus.PartiallyFilled)
                {
                    // Close the position
                    await _positionService.ClosePositionAsync(position.Id, result.Price);

                    // Send notification
                    await _notificationService.SendOrderExecutionNotificationAsync(
                        position.Symbol,
                        result);

                    _logger.LogWarning("Emergency exit executed for position {Id} on {Symbol} at {Price}",
                        position.Id, position.Symbol, result.Price);
                }
                else
                {
                    _logger.LogError("Emergency exit failed for position {Id} on {Symbol}: {Status}",
                        position.Id, position.Symbol, result.Status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during emergency exit for position {Id} on {Symbol}",
                    position.Id, position.Symbol);
            }
        }

        /// <summary>
        /// Calculates dynamic trailing stop loss based on current price and profit
        /// </summary>
        private decimal? CalculateDynamicStopLoss(Position position, decimal currentPrice, decimal pnlPercentage)
        {
            if (position.Type != PositionType.Long)
            {
                return position.StopLoss; // Only handling long positions here
            }

            decimal? newStopLoss = position.StopLoss;

            // If in profit, implement trailing stop loss
            if (pnlPercentage > 0.02m) // 2% profit
            {
                // Calculate new stop loss at break-even
                newStopLoss = position.EntryPrice;
            }

            // If in significant profit, trail with percentage of gains
            if (pnlPercentage > 0.05m) // 5% profit
            {
                // Lock in 50% of the gains
                newStopLoss = position.EntryPrice + (currentPrice - position.EntryPrice) * 0.5m;
            }

            // Never move stop loss downward
            if (position.StopLoss.HasValue && newStopLoss < position.StopLoss)
            {
                newStopLoss = position.StopLoss;
            }

            return newStopLoss;
        }

        /// <summary>
        /// Checks if the market is experiencing high volatility
        /// </summary>
        private async Task<bool> IsMarketVolatileAsync()
        {
            try
            {
                // Use BTC volatility as proxy for market volatility
                var volatility = await CalculateSymbolVolatilityAsync("BTCUSDT", "1h", 24);

                // Check if volatility is significantly higher than normal
                return volatility > 0.04m; // 4% hourly volatility is very high
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking market volatility");
                return false;
            }
        }

        /// <summary>
        /// Checks if the market has a strong directional trend
        /// </summary>
        private async Task<bool> IsMarketTrendingAsync()
        {
            try
            {
                // Use BTC as proxy for market trend
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-3);

                var candles = await _marketDataRepository.GetCandlesticksAsync("BTCUSDT", "4h", startDate, endDate);

                if (candles.Count < 6)
                {
                    return false;
                }

                // Simple trend detection using last 6 candles
                var closes = candles.TakeLast(6).Select(c => c.Close).ToList();
                int upCandles = 0;
                int downCandles = 0;

                for (int i = 1; i < closes.Count; i++)
                {
                    if (closes[i] > closes[i - 1]) upCandles++;
                    else if (closes[i] < closes[i - 1]) downCandles++;
                }

                // Consider trending if 5 out of 6 candles move in the same direction
                return upCandles >= 5 || downCandles >= 5;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking market trend");
                return false;
            }
        }

        /// <summary>
        /// Monitors and rebalances portfolio exposure if needed
        /// </summary>
        private async Task CheckPortfolioExposureAsync()
        {
            try
            {
                var exposure = await CalculatePortfolioExposureAsync();

                _logger.LogInformation("Current portfolio exposure: {Exposure:P2}", exposure);

                // If exposure exceeds critical threshold, reduce positions
                if (exposure > _config.CriticalExposureThreshold)
                {
                    _logger.LogWarning("Critical exposure threshold exceeded: {Exposure:P2} > {Threshold:P2}",
                        exposure, _config.CriticalExposureThreshold);

                    await ReducePortfolioExposureAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking portfolio exposure");
            }
        }

        /// <summary>
        /// Reduces portfolio exposure by closing some positions
        /// </summary>
        private async Task ReducePortfolioExposureAsync()
        {
            try
            {
                // Get open positions
                var openPositions = await _positionService.GetOpenPositionsAsync();

                if (openPositions.Count == 0)
                {
                    return;
                }

                // Sort positions: close worst performing first
                var sortedPositions = new List<Position>();

                foreach (var position in openPositions)
                {
                    var currentPrice = await _exchangeService.GetCurrentPriceAsync(position.Symbol);
                    var pnl = position.CalculatePnl(currentPrice);
                    var pnlPercentage = position.EntryPrice > 0 ? pnl / (position.EntryPrice * position.Quantity) : 0;

                    sortedPositions.Add(position);
                    position.Profit = pnl; // Temporarily store current PnL for sorting
                }

                // Sort by P&L ascending (worst first)
                sortedPositions = sortedPositions
                    .OrderBy(p => p.Profit)
                    .ToList();

                // Close positions until exposure is reduced to acceptable level
                decimal targetExposure = _config.MaxPortfolioExposure * 0.8m; // 80% of max
                decimal currentExposure = await CalculatePortfolioExposureAsync();

                foreach (var position in sortedPositions)
                {
                    if (currentExposure <= targetExposure)
                    {
                        break;
                    }

                    // Close position
                    await EmergencyExitPositionAsync(position);

                    // Recalculate exposure
                    currentExposure = await CalculatePortfolioExposureAsync();

                    _logger.LogWarning("Closed position {Id} on {Symbol} to reduce exposure to {Exposure:P2}",
                        position.Id, position.Symbol, currentExposure);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reducing portfolio exposure");
            }
        }
    }
}