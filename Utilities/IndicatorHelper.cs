
using System;
using System.Collections.Generic;
using System.Linq;

namespace BinanceTradingBot.Utilities
{
    public static class IndicatorHelper
    {
        public static List<decimal> CalculateRsi(List<decimal> prices, int period)
        {
            var rsis = new List<decimal>();
            for (int i = period; i < prices.Count; i++)
            {
                var gains = 0m;
                var losses = 0m;
                for (int j = i - period + 1; j <= i; j++)
                {
                    var change = prices[j] - prices[j - 1];
                    if (change >= 0) gains += change; else losses -= change;
                }
                var rs = gains / (losses == 0 ? 1 : losses);
                var rsi = 100 - (100 / (1 + rs));
                rsis.Add(Math.Round(rsi, 2));
            }
            return rsis;
        }

        public static (List<decimal> macd, List<decimal> signal) CalculateMacd(List<decimal> prices, int fast, int slow, int signal)
        {
            var emaFast = CalculateEma(prices, fast);
            var emaSlow = CalculateEma(prices, slow);
            var macdLine = emaFast.Zip(emaSlow, (f, s) => f - s).ToList();
            var signalLine = CalculateEma(macdLine, signal);
            return (macdLine, signalLine);
        }

        public static (List<decimal> upper, List<decimal> mid, List<decimal> lower) CalculateBollingerBands(List<decimal> prices, int period, double stdDev)
        {
            var mid = CalculateSma(prices, period);
            var upper = new List<decimal>();
            var lower = new List<decimal>();
            for (int i = period - 1; i < prices.Count; i++)
            {
                var range = prices.Skip(i - period + 1).Take(period).ToList();
                var avg = mid[i - period + 1];
                var sd = (decimal)Math.Sqrt(range.Average(v => Math.Pow((double)(v - avg), 2)));
                upper.Add(avg + (decimal)stdDev * sd);
                lower.Add(avg - (decimal)stdDev * sd);
            }
            return (upper, mid.Skip(period - 1).ToList(), lower);
        }

        public static List<decimal> CalculateSma(List<decimal> prices, int period)
        {
            var sma = new List<decimal>();
            for (int i = period - 1; i < prices.Count; i++)
            {
                var avg = prices.Skip(i - period + 1).Take(period).Average();
                sma.Add(Math.Round(avg, 4));
            }
            return sma;
        }

        public static List<decimal> CalculateEma(List<decimal> prices, int period)
        {
            var ema = new List<decimal>();
            decimal k = 2m / (period + 1);
            decimal emaPrev = prices.Take(period).Average();
            ema.Add(emaPrev);

            for (int i = period; i < prices.Count; i++)
            {
                emaPrev = prices[i] * k + emaPrev * (1 - k);
                ema.Add(Math.Round(emaPrev, 4));
            }

            return ema;
        }
    }
}
