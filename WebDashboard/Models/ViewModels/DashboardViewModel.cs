using BinanceTradingBot.WebDashboard.Models.DTOs;

namespace BinanceTradingBot.WebDashboard.Models
{
    public class DashboardViewModel
    {
        // Résumé général
        public int TotalPositions { get; set; }
        public int OpenPositions { get; set; }
        public int ClosedPositions { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal DailyProfit { get; set; }
        public decimal WeeklyProfit { get; set; }
        public decimal MonthlyProfit { get; set; }

        // Statistiques de performance
        public decimal WinRate { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal AverageProfit { get; set; }
        public decimal AverageLoss { get; set; }
        public decimal ProfitFactor { get; set; }
        public decimal MaxDrawdown { get; set; }

        // État actuel
        public IEnumerable<PositionDTO> RecentPositions { get; set; } = new List<PositionDTO>();
        public Dictionary<string, decimal> AssetBalances { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> CurrentPrices { get; set; } = new Dictionary<string, decimal>();

        // Statut du bot
        public bool IsRunning { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string CurrentStrategy { get; set; } = string.Empty;
        public int ActiveTradingPairs { get; set; }
    }
}