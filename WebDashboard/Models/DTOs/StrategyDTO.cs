namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    public class StrategyDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}