using System.ComponentModel.DataAnnotations;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    public class StopLossTakeProfitUpdateDTO
    {
        [Range(0, double.MaxValue, ErrorMessage = "Le stop loss doit être positif")]
        public decimal? StopLoss { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le take profit doit être positif")]
        public decimal? TakeProfit { get; set; }
    }
}