using BinanceTradingBot.WebDashboard.Models.DTOs;

namespace BinanceTradingBot.WebDashboard.Models
{
    public class PerformanceStatsViewModel
    {
        public string Symbol { get; set; } = "ALL";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal TotalProfit { get; set; }
        public decimal WinRate { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal AverageProfit { get; set; }
        public decimal MaxProfit { get; set; }
        public decimal MaxLoss { get; set; }
        public decimal ProfitFactor { get; set; }
        public decimal SharpeRatio { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal MaxDrawdownPercentage { get; set; }
        public int AverageHoldingPeriodHours { get; set; }

        // Données pour les graphiques
        public List<PositionDTO> Positions { get; set; } = new List<PositionDTO>();
        public List<KeyValuePair<DateTime, decimal>> EquityCurve { get; set; } = new List<KeyValuePair<DateTime, decimal>>();
        public List<KeyValuePair<string, int>> PositionsByStrategy { get; set; } = new List<KeyValuePair<string, int>>();
    }
}