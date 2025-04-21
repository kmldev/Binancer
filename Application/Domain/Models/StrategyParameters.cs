using System.Collections.Generic;

namespace BinanceTradingBot.Domain.Models
{
    public class StrategyParameters
    {
        public int RsiPeriod { get; set; } = 14;
        public int RsiOversold { get; set; } = 30;
        public int RsiOverbought { get; set; } = 70;
        public int MacdFastPeriod { get; set; } = 12;
        public int MacdSlowPeriod { get; set; } = 26;
        public int MacdSignalPeriod { get; set; } = 9;
        public int BbPeriod { get; set; } = 20;
        public double BbStdDev { get; set; } = 2.0;
        public double BbWidthThreshold { get; set; } = 0.05;
        public double StopLossPercentage { get; set; } = 0.02;
        public double TakeProfitPercentage { get; set; } = 0.05;
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
    }
}