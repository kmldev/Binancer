using System;
using System.Collections.Generic;
using System.Linq;
using BinanceTradingBot.Domain.Enums;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Domain.Models;
using BinanceTradingBot.Utilities;

namespace BinanceTradingBot.Application.Strategies
{
    /// <summary>
    /// Moving Average Crossover strategy implementation
    /// </summary>
    public class MACrossStrategy
    {
        private readonly StrategyParameters _params;
        private readonly List<CandlestickData> _candles;

        /// <summary>
        /// Initializes a new instance of the MACrossStrategy class.
        /// </summary>
        /// <param name="parameters">Strategy parameters</param>
        /// <param name="candles">Candlestick data</param>
        public MACrossStrategy(StrategyParameters parameters, List<CandlestickData> candles)
        {
            _params = parameters;
            _candles = candles;

            // Set default parameters if not specified
            if (!_params.CustomParameters.ContainsKey("FastMA"))
            {
                _params.CustomParameters["FastMA"] = 9;
            }

            if (!_params.CustomParameters.ContainsKey("SlowMA"))
            {
                _params.CustomParameters["SlowMA"] = 21;
            }

            if (!_params.CustomParameters.ContainsKey("MAType"))
            {
                _params.CustomParameters["MAType"] = "EMA"; // Options: "SMA", "EMA"
            }

            if (!_params.CustomParameters.ContainsKey("VolumeThreshold"))
            {
                _params.CustomParameters["VolumeThreshold"] = 1.5; // Volume must be 1.5x average
            }
        }

        /// <summary>
        /// Generates a trading signal based on moving average crossover
        /// </summary>
        /// <returns>Trading signal with action, price, and confidence</returns>
        public TradingSignal GenerateSignal()
        {
            var closePrices = _candles.Select(c => c.Close).ToList();
            var volumes = _candles.Select(c => c.Volume).ToList();
            var latest = _candles.Last();

            // Get strategy parameters
            int fastPeriod = Convert.ToInt32(_params.CustomParameters["FastMA"]);
            int slowPeriod = Convert.ToInt32(_params.CustomParameters["SlowMA"]);
            string maType = _params.CustomParameters["MAType"].ToString();
            double volumeThreshold = Convert.ToDouble(_params.CustomParameters["VolumeThreshold"]);

            // Calculate moving averages
            List<decimal> fastMA;
            List<decimal> slowMA;

            if (maType == "EMA")
            {
                fastMA = IndicatorHelper.CalculateEma(closePrices, fastPeriod);
                slowMA = IndicatorHelper.CalculateEma(closePrices, slowPeriod);
            }
            else
            {
                fastMA = IndicatorHelper.CalculateSma(closePrices, fastPeriod);
                slowMA = IndicatorHelper.CalculateSma(closePrices, slowPeriod);
            }

            // Check for enough data
            if (fastMA.Count < 2 || slowMA.Count < 2)
            {
                return new TradingSignal { Action = SignalAction.None };
            }

            // Get current and previous values
            decimal currentFastMA = fastMA.Last();
            decimal currentSlowMA = slowMA.Last();
            decimal previousFastMA = fastMA[fastMA.Count - 2];
            decimal previousSlowMA = slowMA[slowMA.Count - 2];

            // Check for crossover
            bool crossedAbove = previousFastMA <= previousSlowMA && currentFastMA > currentSlowMA;
            bool crossedBelow = previousFastMA >= previousSlowMA && currentFastMA < currentSlowMA;

            // Check volume condition (current volume > average * threshold)
            decimal avgVolume = volumes.Skip(Math.Max(0, volumes.Count - 20)).Take(20).Average();
            bool highVolume = latest.Volume > avgVolume * (decimal)volumeThreshold;

            // Check trend direction
            bool uptrend = closePrices.Skip(Math.Max(0, closePrices.Count - 10)).All(p => p >= currentSlowMA * 0.97m);
            bool downtrend = closePrices.Skip(Math.Max(0, closePrices.Count - 10)).All(p => p <= currentSlowMA * 1.03m);

            // Calculate RSI for confirmation
            var rsi = IndicatorHelper.CalculateRsi(closePrices, _params.RsiPeriod);
            decimal latestRsi = rsi.LastOrDefault();

            // Prepare signal
            var signal = new TradingSignal
            {
                Action = SignalAction.None,
                Price = latest.Close,
                Confidence = 0.5
            };

            // Add indicators to signal for logging
            signal.Indicators["FastMA"] = currentFastMA;
            signal.Indicators["SlowMA"] = currentSlowMA;
            signal.Indicators["RSI"] = latestRsi;
            signal.Indicators["Volume"] = latest.Volume;
            signal.Indicators["AvgVolume"] = avgVolume;
            signal.Indicators["CrossAbove"] = crossedAbove;
            signal.Indicators["CrossBelow"] = crossedBelow;

            // Generate buy signal
            if (crossedAbove && highVolume && uptrend && latestRsi < 70)
            {
                signal.Action = SignalAction.Buy;
                signal.Confidence = CalculateConfidence(crossedAbove, highVolume, uptrend, latestRsi < 70);
                return signal;
            }

            // Generate sell signal
            if (crossedBelow && highVolume && downtrend && latestRsi > 30)
            {
                signal.Action = SignalAction.Sell;
                signal.Confidence = CalculateConfidence(crossedBelow, highVolume, downtrend, latestRsi > 30);
                return signal;
            }

            return signal;
        }

        /// <summary>
        /// Calculates signal confidence based on conditions met
        /// </summary>
        private double CalculateConfidence(params bool[] conditions)
        {
            int trueCount = conditions.Count(c => c);
            double baseConfidence = 0.6;
            return Math.Min(baseConfidence + (trueCount * 0.1), 0.95);
        }
    }
}