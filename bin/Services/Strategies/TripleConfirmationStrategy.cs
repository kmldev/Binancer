using System.Collections.Generic;
using System.Linq;
using BinanceTradingBot.Domain.Enums;
using BinanceTradingBot.Domain.Models;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Utilities;

namespace BinanceTradingBot.Application.Strategies
{
    public class TripleConfirmationStrategy
    {
        private readonly StrategyParameters _params;
        private readonly List<CandlestickData> _candles;

        public TripleConfirmationStrategy(StrategyParameters parameters, List<CandlestickData> candles)
        {
            _params = parameters;
            _candles = candles;
        }

        public TradingSignal GenerateSignal()
        {
            var latest = _candles.Last();
            var closePrices = _candles.Select(c => c.Close).ToList();

            var rsi = IndicatorHelper.CalculateRsi(closePrices, _params.RsiPeriod).Last();
            var (macdLine, signalLine) = IndicatorHelper.CalculateMacd(closePrices, _params.MacdFastPeriod, _params.MacdSlowPeriod, _params.MacdSignalPeriod);
            var (upper, middle, lower) = IndicatorHelper.CalculateBollingerBands(closePrices, _params.BbPeriod, (double)_params.BbStdDev);
            var bbWidth = (upper.Last() - lower.Last()) / middle.Last();

            bool breakout = latest.Close > upper.Last();
            bool macdBull = macdLine.Last() > signalLine.Last();
            bool rsiLow = rsi < (decimal)_params.RsiOversold;
            bool bbSqueeze = bbWidth < (decimal)_params.BbWidthThreshold;

            if (breakout && macdBull && rsiLow && bbSqueeze)
            {
                return new TradingSignal { Action = SignalAction.Buy, Price = latest.Close, Confidence = 0.9 };
            }

            return new TradingSignal { Action = SignalAction.None };
        }
    }
}
