using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BinanceTradingBot.Application.Interfaces;
using BinanceTradingBot.Application.Strategies;
using BinanceTradingBot.Domain.Enums;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Utilities;

namespace BinanceTradingBot.Application.Services
{
    /// <summary>
    /// Service for backtesting trading strategies on historical data
    /// </summary>
    public class BacktestService
    {
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly ILogger<BacktestService> _logger;
        private readonly AppSettings _config;

        public BacktestService(
            IMarketDataRepository marketDataRepository,
            ILogger<BacktestService> logger,
            IOptions<AppSettings> config)
        {
            _marketDataRepository = marketDataRepository;
            _logger = logger;
            _config = config.Value;
        }

        /// <summary>
        /// Runs a backtest for a specific strategy on historical data
        /// </summary>
        public async Task<BinanceTradingBot.Domain.Models.StrategyPerformance> RunBacktestAsync(
            string symbol,
            string interval,
            string strategyName,
            DateTime startDate,
            DateTime endDate,
            Dictionary<string, object>? parameters = null)
        {
            try
            {
                _logger.LogInformation("Running backtest for {Symbol} using {Strategy} from {Start} to {End}",
                    symbol, strategyName, startDate, endDate);

                // Get historical data
                var candles = await _marketDataRepository.GetCandlesticksAsync(symbol, interval, startDate, endDate);

                if (candles.Count < 100)
                {
                    throw new Exception($"Not enough historical data for backtest. Found {candles.Count} candles.");
                }

                // Configure strategy parameters
                var strategyParams = new BinanceTradingBot.Domain.Models.StrategyParameters();

                // Apply default parameters
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        if (typeof(BinanceTradingBot.Domain.Models.StrategyParameters).GetProperty(param.Key) != null)
                        {
                            typeof(BinanceTradingBot.Domain.Models.StrategyParameters).GetProperty(param.Key).SetValue(strategyParams, param.Value);
                        }
                        else
                        {
                            strategyParams.CustomParameters[param.Key] = param.Value;
                        }
                    }
                }

                // Initialize performance metrics
                var performance = new BinanceTradingBot.Domain.Models.StrategyPerformance
                {
                    Symbol = symbol,
                    Strategy = strategyName,
                    Interval = interval,
                    StartDate = startDate,
                    EndDate = endDate
                };

                // Initial capital and position tracking
                decimal initialCapital = 10000m; // Start with $10,000
                decimal availableCapital = initialCapital;
                decimal positionSize = 0m;
                decimal entryPrice = 0m;
                bool inPosition = false;
                PositionType positionType = PositionType.Long;

                // Track trades for performance analysis
                var trades = new List<BacktestTrade>();
                decimal highestCapital = initialCapital;
                decimal lowestCapital = initialCapital;
                decimal totalWinAmount = 0m;
                decimal totalLossAmount = 0m;

                // Minimum number of candles required for strategy calculation
                int warmupPeriod = 50;

                // Run the simulation
                for (int i = warmupPeriod; i < candles.Count; i++)
                {
                    // Get the current window of data for analysis
                    var window = candles.Take(i + 1).ToList();
                    var currentCandle = window.Last();
                    var currentPrice = currentCandle.Close;

                    // Generate signal based on selected strategy
                    BinanceTradingBot.Domain.Models.TradingSignal signal;
                    switch (strategyName.ToLower())
                    {
                        case "tripleconfirmation":
                            var tripleStrategy = new TripleConfirmationStrategy(strategyParams, window);
                            signal = tripleStrategy.GenerateSignal();
                            break;
                        case "macross":
                            var maStrategy = new MACrossStrategy(strategyParams, window);
                            signal = maStrategy.GenerateSignal();
                            break;
                        default:
                            signal = new BinanceTradingBot.Domain.Models.TradingSignal { Action = SignalAction.None };
                            break;
                    }

                    // Update signal with current price and metadata
                    signal.Price = currentPrice;
                    signal.Symbol = symbol;
                    signal.Timestamp = currentCandle.OpenTime;
                    signal.Strategy = strategyName;

                    // Process signals
                    if (!inPosition && signal.Action == SignalAction.Buy)
                    {
                        // Calculate position size (use a percentage of available capital)
                        decimal riskAmount = availableCapital * (decimal)_config.RiskPerTradePercentage;
                        positionSize = riskAmount / currentPrice;
                        entryPrice = currentPrice;
                        availableCapital -= riskAmount;
                        inPosition = true;
                        positionType = PositionType.Long;

                        _logger.LogDebug("BUY signal at {Time}: Opened position at {Price}",
                            currentCandle.OpenTime, currentPrice);
                    }
                    else if (inPosition && signal.Action == SignalAction.Sell)
                    {
                        // Close the position
                        decimal exitValue = positionSize * currentPrice;
                        decimal profitLoss = 0m;

                        if (positionType == PositionType.Long)
                        {
                            profitLoss = positionSize * (currentPrice - entryPrice);
                        }
                        else
                        {
                            profitLoss = positionSize * (entryPrice - currentPrice);
                        }

                        availableCapital += exitValue;
                        inPosition = false;

                        // Record the trade
                        var trade = new BacktestTrade
                        {
                            EntryTime = currentCandle.OpenTime.AddMinutes(-GetIntervalMinutes(interval)),
                            EntryPrice = entryPrice,
                            ExitTime = currentCandle.OpenTime,
                            ExitPrice = currentPrice,
                            ProfitLoss = profitLoss,
                            PositionType = positionType
                        };
                        trades.Add(trade);

                        // Update performance statistics
                        if (profitLoss > 0)
                        {
                            performance.WinningTrades++;
                            totalWinAmount += profitLoss;
                        }
                        else
                        {
                            performance.LosingTrades++;
                            totalLossAmount += Math.Abs(profitLoss);
                        }

                        _logger.LogDebug("SELL signal at {Time}: Closed position at {Price}, P/L: {ProfitLoss}",
                            currentCandle.OpenTime, currentPrice, profitLoss);
                    }

                    // Check for stop loss and take profit (if position is open)
                    if (inPosition)
                    {
                        // Calculate current value and check stop loss/take profit
                        decimal currentProfitLoss = 0m;
                        if (positionType == PositionType.Long)
                        {
                            currentProfitLoss = positionSize * (currentPrice - entryPrice);

                            // Check stop loss
                            if (_config.UseStopLoss &&
                                currentPrice <= entryPrice * (1 - (decimal)_config.StopLossPercentage))
                            {
                                decimal stopPrice = entryPrice * (1 - (decimal)_config.StopLossPercentage);
                                decimal exitValue = positionSize * stopPrice;
                                decimal realizedLoss = positionSize * (stopPrice - entryPrice);

                                availableCapital += exitValue;
                                inPosition = false;

                                var trade = new BacktestTrade
                                {
                                    EntryTime = currentCandle.OpenTime.AddMinutes(-GetIntervalMinutes(interval)),
                                    EntryPrice = entryPrice,
                                    ExitTime = currentCandle.OpenTime,
                                    ExitPrice = stopPrice,
                                    ProfitLoss = realizedLoss,
                                    PositionType = positionType,
                                    ExitReason = "Stop Loss"
                                };
                                trades.Add(trade);

                                performance.LosingTrades++;
                                totalLossAmount += Math.Abs(realizedLoss);

                                _logger.LogDebug("STOP LOSS at {Time}: Closed position at {Price}, Loss: {Loss}",
                                    currentCandle.OpenTime, stopPrice, realizedLoss);
                            }

                            // Check take profit
                            else if (_config.UseTakeProfit &&
                                    currentPrice >= entryPrice * (1 + (decimal)_config.TakeProfitPercentage))
                            {
                                decimal takePrice = entryPrice * (1 + (decimal)_config.TakeProfitPercentage);
                                decimal exitValue = positionSize * takePrice;
                                decimal realizedProfit = positionSize * (takePrice - entryPrice);

                                availableCapital += exitValue;
                                inPosition = false;

                                var trade = new BacktestTrade
                                {
                                    EntryTime = currentCandle.OpenTime.AddMinutes(-GetIntervalMinutes(interval)),
                                    EntryPrice = entryPrice,
                                    ExitTime = currentCandle.OpenTime,
                                    ExitPrice = takePrice,
                                    ProfitLoss = realizedProfit,
                                    PositionType = positionType,
                                    ExitReason = "Take Profit"
                                };
                                trades.Add(trade);

                                performance.WinningTrades++;
                                totalWinAmount += realizedProfit;

                                _logger.LogDebug("TAKE PROFIT at {Time}: Closed position at {Price}, Profit: {Profit}",
                                    currentCandle.OpenTime, takePrice, realizedProfit);
                            }
                        }
                        else
                        {
                            // Similar logic for short positions (not implemented here)
                        }
                    }

                    // Track capital high/low
                    decimal currentCapital = availableCapital;
                    if (inPosition)
                    {
                        currentCapital += positionSize * currentPrice;
                    }

                    highestCapital = Math.Max(highestCapital, currentCapital);
                    lowestCapital = Math.Min(lowestCapital, currentCapital);
                }

                // Close any open position at the end
                if (inPosition)
                {
                    var lastCandle = candles.Last();
                    decimal exitValue = positionSize * lastCandle.Close;
                    decimal profitLoss = 0m;

                    if (positionType == PositionType.Long)
                    {
                        profitLoss = positionSize * (lastCandle.Close - entryPrice);
                    }
                    else
                    {
                        profitLoss = positionSize * (entryPrice - lastCandle.Close);
                    }

                    availableCapital += exitValue;

                    var trade = new BacktestTrade
                    {
                        EntryTime = lastCandle.OpenTime.AddMinutes(-GetIntervalMinutes(interval)),
                        EntryPrice = entryPrice,
                        ExitTime = lastCandle.OpenTime,
                        ExitPrice = lastCandle.Close,
                        ProfitLoss = profitLoss,
                        PositionType = positionType,
                        ExitReason = "End of Backtest"
                    };
                    trades.Add(trade);

                    if (profitLoss > 0)
                    {
                        performance.WinningTrades++;
                        totalWinAmount += profitLoss;
                    }
                    else
                    {
                        performance.LosingTrades++;
                        totalLossAmount += Math.Abs(profitLoss);
                    }
                }

                // Calculate final performance metrics
                performance.TotalTrades = trades.Count;
                performance.TotalProfit = availableCapital - initialCapital;

                if (performance.TotalTrades > 0)
                {
                    performance.AverageProfit = trades.Sum(t => t.ProfitLoss) / trades.Count;
                }

                performance.MaxDrawdown = highestCapital > 0
                    ? (highestCapital - lowestCapital) / highestCapital
                    : 0;

                performance.ProfitFactor = totalLossAmount > 0
                    ? totalWinAmount / totalLossAmount
                    : totalWinAmount;

                // Calculate Sharpe Ratio (annualized)
                if (trades.Count > 0)
                {
                    var returns = trades.Select(t => t.ProfitLoss / initialCapital).ToList();
                    var avgReturn = returns.Average();
                    var stdDev = Math.Sqrt(returns.Sum(r => Math.Pow((double)(r - avgReturn), 2)) / returns.Count);

                    // Annualized Sharpe Ratio (assuming 252 trading days)
                    var annualFactor = 252.0 / ((endDate - startDate).TotalDays / GetIntervalDays(interval));
                    performance.SharpeRatio = stdDev > 0
                        ? (decimal)avgReturn / (decimal)stdDev * (decimal)Math.Sqrt(annualFactor)
                        : 0;
                }

                _logger.LogInformation("Backtest completed for {Symbol} using {Strategy}: {Trades} trades, {Profit:C} profit",
                    symbol, strategyName, performance.TotalTrades, performance.TotalProfit);

                return performance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running backtest for {Symbol} using {Strategy}", symbol, strategyName);
                throw;
            }
        }

        /// <summary>
        /// Helper method to convert interval string to minutes
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

        /// <summary>
        /// Helper method to convert interval string to days (for annualization)
        /// </summary>
        private double GetIntervalDays(string interval)
        {
            return GetIntervalMinutes(interval) / 1440.0;
        }
    }

    /// <summary>
    /// Model to track individual trades during backtesting
    /// </summary>
    public class BacktestTrade
    {
        public DateTime EntryTime { get; set; }
        public decimal EntryPrice { get; set; }
        public DateTime ExitTime { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal ProfitLoss { get; set; }
        public PositionType PositionType { get; set; }
        public string ExitReason { get; set; } = "Signal";

        public decimal ReturnPercentage => EntryPrice > 0
            ? (ExitPrice / EntryPrice - 1) * 100
            : 0;

        public TimeSpan Duration => ExitTime - EntryTime;
    }
}