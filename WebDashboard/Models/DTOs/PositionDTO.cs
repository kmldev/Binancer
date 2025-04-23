using BinanceTradingBot.Domain.Enums;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    public class PositionDTO
    {
        public long Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public PositionType Type { get; set; }
        public PositionStatus Status { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal? ExitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal? TakeProfit { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime? CloseTime { get; set; }
        public decimal? Profit { get; set; }
        public string Strategy { get; set; } = string.Empty;
        public bool IsClosed => Status == PositionStatus.Closed;
        public decimal CurrentProfit { get; set; }
        public decimal CurrentProfitPercentage { get; set; }
    }
}