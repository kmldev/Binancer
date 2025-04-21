using System;

namespace BinanceTradingBot.Domain.Models
{
    public class StrategyPerformance
    {
        public string Symbol { get; set; }
        public string Strategy { get; set; }
        public string Interval { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal AverageProfit { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal ProfitFactor { get; set; }
        public decimal SharpeRatio { get; set; }
        public decimal WinRate => TotalTrades > 0 ? (decimal)WinningTrades / TotalTrades : 0;
    }
}