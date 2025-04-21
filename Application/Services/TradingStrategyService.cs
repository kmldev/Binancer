using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BinanceTradingBot.Application.Interfaces;
using BinanceTradingBot.Domain.Enums;
using BinanceTradingBot.Domain.Models;
using BinanceTradingBot.Domain.Entities; // Added to use CandlestickData from Entities
using BinanceTradingBot.Utilities;

namespace BinanceTradingBot.Application.Services
{
    /// <summary>
    /// Service pour l'exécution et la gestion des stratégies de trading
    /// </summary>
    public class TradingStrategyService : IStrategyService
    {
        private readonly IExchangeService _exchangeService;
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly ILogger<TradingStrategyService> _logger;
        private readonly AppSettings _config;
        private StrategyParameters _parameters;
        private const string DefaultStrategyName = "TripleConfirmation";

        public TradingStrategyService(
            IExchangeService exchangeService,
            IMarketDataRepository marketDataRepository,
            ILogger<TradingStrategyService> logger,
            IOptions<AppSettings> config)
        {
            _exchangeService = exchangeService;
            _marketDataRepository = marketDataRepository;
            _logger = logger;
            _config = config.Value;
            _parameters = new StrategyParameters();
        }

        /// <summary>
        /// Génère un signal de trading basé sur la stratégie configurée
        /// </summary>
        public async Task<TradingSignal> GenerateSignalAsync(string symbol, string interval)
        {
            try
            {
                _logger.LogInformation($"Generating signal for {symbol} on {interval} timeframe");

                // Récupérer les données de marché nécessaires
                var candles = await GetMarketDataAsync(symbol, interval);

                if (candles.Count < 100)
                {
                    _logger.LogWarning($"Not enough candlestick data for {symbol}. Need at least 100 candles.");
                    return new TradingSignal { Symbol = symbol, Action = SignalAction.None };
                }

                // Récupérer le prix actuel
                var currentPrice = await _exchangeService.GetCurrentPriceAsync(symbol);

                // Générer le signal en fonction de la stratégie configurée
                var signal = GenerateSignalByStrategy(symbol, interval, candles, currentPrice);

                // Enregistrement des indicateurs et des logs
                _logger.LogInformation($"Signal generated for {symbol}: {signal.Action} at {signal.Price} with confidence {signal.Confidence}");

                return signal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating signal for {symbol}");
                return new TradingSignal { Symbol = symbol, Action = SignalAction.None };
            }
        }

        /// <summary>
        /// Évalue la performance d'une stratégie sur des données historiques
        /// </summary>
        public async Task<StrategyPerformance> BacktestStrategyAsync(string symbol, string interval, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation($"Starting backtest for {symbol} from {startDate} to {endDate}");

                // Récupérer les données historiques de marché
                var candles = await _marketDataRepository.GetCandlesticksAsync(symbol, interval, startDate, endDate);

                if (candles.Count < 100)
                {
                    throw new Exception($"Not enough historical data for backtest. Found {candles.Count} candles.");
                }

                var performance = new StrategyPerformance
                {
                    Symbol = symbol,
                    Strategy = "TripleConfirmation",
                    Interval = interval,
                    StartDate = startDate,
                    EndDate = endDate
                };

                // Initialiser les variables de simulation
                decimal initialBalance = 1000; // Simulation avec 1000 unités
                decimal currentBalance = initialBalance;
                decimal currentPrice = 0;
                bool inPosition = false;
                decimal entryPrice = 0;
                decimal highestBalance = initialBalance;
                decimal lowestBalance = initialBalance;
                decimal totalProfit = 0;
                decimal totalLoss = 0;
                var trades = new List<decimal>();

                // Parcourir les données historiques
                for (int i = 100; i < candles.Count; i++)
                {
                    var historicalCandles = candles.Take(i).ToList();
                    currentPrice = historicalCandles.Last().Close;

                    var signal = GenerateSignalByStrategy(symbol, interval, historicalCandles, currentPrice);

                    // Simuler les trades
                    if (!inPosition && signal.Action == SignalAction.Buy)
                    {
                        entryPrice = currentPrice;
                        inPosition = true;
                        performance.TotalTrades++;
                    }
                    else if (inPosition)
                    {
                        // FIX: Convert double to decimal using 'm' suffix
                        bool takeProfit = currentPrice >= entryPrice * (1 + (decimal)_parameters.TakeProfitPercentage);
                        bool stopLoss = currentPrice <= entryPrice * (1 - (decimal)_parameters.StopLossPercentage);
                        bool sellSignal = signal.Action == SignalAction.Sell;

                        if (takeProfit || stopLoss || sellSignal)
                        {
                            decimal profit = ((currentPrice / entryPrice) - 1) * currentBalance;
                            currentBalance += profit;

                            if (profit > 0)
                            {
                                performance.WinningTrades++;
                                totalProfit += profit;
                            }
                            else
                            {
                                performance.LosingTrades++;
                                totalLoss += Math.Abs(profit);
                            }

                            trades.Add(profit);
                            highestBalance = Math.Max(highestBalance, currentBalance);
                            lowestBalance = Math.Min(lowestBalance, currentBalance);
                            inPosition = false;
                        }
                    }
                }

                // Calculer les métriques de performance
                performance.TotalProfit = currentBalance - initialBalance;
                performance.AverageProfit = trades.Count > 0 ? trades.Average() : 0;
                
                // FIX: Fix the decimal division operation
                performance.MaxDrawdown = highestBalance > 0 ? (highestBalance - lowestBalance) / highestBalance : 0;
                performance.ProfitFactor = totalLoss > 0 ? totalProfit / totalLoss : totalProfit;

                // Calculer le ratio de Sharpe
                if (trades.Count > 0)
                {
                    var returns = trades.Select(t => t / initialBalance).ToList();
                    var avgReturn = returns.Average();
                    var variance = returns.Sum(r => (r - avgReturn) * (r - avgReturn)) / (decimal)returns.Count;
                    var stdDev = Math.Sqrt((double)variance);
                    
                    // FIX: Use decimal cast for the division operation
                    performance.SharpeRatio = stdDev > 0 ? (decimal)(double)avgReturn / (decimal)stdDev * (decimal)Math.Sqrt(252) : 0; // Annualisé avec 252 jours de trading
                }

                _logger.LogInformation($"Backtest completed for {symbol}: {performance.TotalTrades} trades with {performance.TotalProfit:F2} profit");

                return performance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during backtest for {symbol}");
                throw;
            }
        }

        /// <summary>
        /// Configure les paramètres de la stratégie
        /// </summary>
        public void ConfigureStrategy(Dictionary<string, object> parameters)
        {
            foreach (var param in parameters)
            {
                switch (param.Key)
                {
                    case "RsiPeriod":
                        _parameters.RsiPeriod = Convert.ToInt32(param.Value);
                        break;
                    case "RsiOversold":
                        _parameters.RsiOversold = Convert.ToInt32(param.Value);
                        break;
                    case "RsiOverbought":
                        _parameters.RsiOverbought = Convert.ToInt32(param.Value);
                        break;
                    case "MacdFastPeriod":
                        _parameters.MacdFastPeriod = Convert.ToInt32(param.Value);
                        break;
                    case "MacdSlowPeriod":
                        _parameters.MacdSlowPeriod = Convert.ToInt32(param.Value);
                        break;
                    case "MacdSignalPeriod":
                        _parameters.MacdSignalPeriod = Convert.ToInt32(param.Value);
                        break;
                    case "BbPeriod":
                        _parameters.BbPeriod = Convert.ToInt32(param.Value);
                        break;
                    case "BbStdDev":
                        _parameters.BbStdDev = Convert.ToDouble(param.Value);
                        break;
                    case "BbWidthThreshold":
                        _parameters.BbWidthThreshold = Convert.ToDouble(param.Value);
                        break;
                    case "StopLossPercentage":
                        _parameters.StopLossPercentage = Convert.ToDouble(param.Value);
                        break;
                    case "TakeProfitPercentage":
                        _parameters.TakeProfitPercentage = Convert.ToDouble(param.Value);
                        break;
                    default:
                        if (_parameters.CustomParameters.ContainsKey(param.Key))
                        {
                            _parameters.CustomParameters[param.Key] = param.Value;
                        }
                        else
                        {
                            _parameters.CustomParameters.Add(param.Key, param.Value);
                        }
                        break;
                }
            }

            _logger.LogInformation("Strategy parameters updated");
        }

        #region Private Methods

        /// <summary>
        /// Récupère les données de marché pour l'analyse
        /// </summary>
        private async Task<List<CandlestickData>> GetMarketDataAsync(string symbol, string interval)
        {
            // Essayer d'abord d'obtenir les données en cache
            var cachedCandles = await _marketDataRepository.GetLatestCandlesticksAsync(symbol, interval, 100);

            if (cachedCandles.Count >= 100)
            {
                return cachedCandles;
            }

            // Si pas assez de données en cache, récupérer depuis l'API
            var candles = await _exchangeService.GetCandlesticksAsync(symbol, interval, 100);

            // Sauvegarder les données récupérées
            await _marketDataRepository.SaveCandlesticksAsync(symbol, interval, candles);

            return candles;
        }

        /// <summary>
        /// Génère un signal de trading basé sur la stratégie sélectionnée
        /// </summary>
        private TradingSignal GenerateSignalByStrategy(string symbol, string interval, List<CandlestickData> candles, decimal currentPrice)
        {
            // Par défaut, utiliser la stratégie TripleConfirmation
            var strategyName = _config.DefaultStrategy ?? DefaultStrategyName;

            var signal = new TradingSignal
            {
                Symbol = symbol,
                Interval = interval,
                Price = currentPrice,
                Strategy = strategyName,
                Timestamp = DateTime.UtcNow
            };

            switch (strategyName)
            {
                case "TripleConfirmation":
                    signal = GenerateTripleConfirmationSignal(candles, currentPrice);
                    break;
                // Autres stratégies peuvent être ajoutées ici
                default:
                    signal = GenerateTripleConfirmationSignal(candles, currentPrice);
                    break;
            }

            signal.Symbol = symbol;
            signal.Interval = interval;
            signal.Strategy = strategyName;

            return signal;
        }

        /// <summary>
        /// Implémentation de la stratégie Triple Confirmation
        /// </summary>
        private TradingSignal GenerateTripleConfirmationSignal(List<CandlestickData> candles, decimal currentPrice)
        {
            var signal = new TradingSignal
            {
                Action = SignalAction.None,
                Price = currentPrice,
                Confidence = 0
            };

            var closePrices = candles.Select(c => c.Close).ToList();
            var lastClose = closePrices.Last();

            // Calcul des indicateurs
            var rsi = IndicatorHelper.CalculateRsi(closePrices, _parameters.RsiPeriod);
            var (macdLine, signalLine) = IndicatorHelper.CalculateMacd(
                closePrices,
                _parameters.MacdFastPeriod,
                _parameters.MacdSlowPeriod,
                _parameters.MacdSignalPeriod);

            var (upperBand, middleBand, lowerBand) = IndicatorHelper.CalculateBollingerBands(
                closePrices,
                _parameters.BbPeriod,
                _parameters.BbStdDev);

            // Récupération des dernières valeurs des indicateurs
            var latestRsi = rsi.LastOrDefault();
            var latestMacd = macdLine.LastOrDefault();
            var latestSignal = signalLine.LastOrDefault();
            var latestUpper = upperBand.LastOrDefault();
            var latestMiddle = middleBand.LastOrDefault();
            var latestLower = lowerBand.LastOrDefault();

            // Calcul de la largeur de la bande de Bollinger
            var bbWidth = (latestUpper - latestLower) / latestMiddle;

            // Vérification des conditions d'achat
            bool rsiOversold = latestRsi < _parameters.RsiOversold;
            bool macdCrossUp = macdLine.Count > 2 &&
                             macdLine[macdLine.Count - 2] < signalLine[signalLine.Count - 2] &&
                             latestMacd > latestSignal;
            bool priceNearLowerBand = lastClose < latestLower * 1.01m;
            
            // FIX: Convert the double to decimal using cast or 'm' suffix
            bool bbSqueeze = bbWidth < (decimal)_parameters.BbWidthThreshold;

            // Vérification des conditions de vente
            bool rsiOverbought = latestRsi > _parameters.RsiOverbought;
            bool macdCrossDown = macdLine.Count > 2 &&
                               macdLine[macdLine.Count - 2] > signalLine[signalLine.Count - 2] &&
                               latestMacd < latestSignal;
            bool priceNearUpperBand = lastClose > latestUpper * 0.99m;

            // Enregistrer les indicateurs pour le logging et la visualisation
            signal.Indicators["RSI"] = latestRsi;
            signal.Indicators["MACD"] = latestMacd;
            signal.Indicators["Signal"] = latestSignal;
            signal.Indicators["UpperBand"] = latestUpper;
            signal.Indicators["MiddleBand"] = latestMiddle;
            signal.Indicators["LowerBand"] = latestLower;
            signal.Indicators["BBWidth"] = bbWidth;

            // Générer le signal d'achat
            if (rsiOversold && macdCrossUp && (priceNearLowerBand || bbSqueeze))
            {
                signal.Action = SignalAction.Buy;
                signal.Confidence = CalculateConfidence(rsiOversold, macdCrossUp, priceNearLowerBand, bbSqueeze);
            }
            // Générer le signal de vente
            else if (rsiOverbought && macdCrossDown && priceNearUpperBand)
            {
                signal.Action = SignalAction.Sell;
                signal.Confidence = CalculateConfidence(rsiOverbought, macdCrossDown, priceNearUpperBand);
            }

            return signal;
        }

        /// <summary>
        /// Calcule un niveau de confiance basé sur le nombre de conditions remplies
        /// </summary>
        private double CalculateConfidence(params bool[] conditions)
        {
            int trueCount = conditions.Count(c => c);
            
            // FIX: Make sure all literals are the same type (double)
            return Math.Min(0.5 + (trueCount * 0.1), 0.95);
        }

        #endregion
    }
}