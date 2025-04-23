using System.ComponentModel.DataAnnotations;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    public class TradingPairDTO
    {
        [Required]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        public string BaseAsset { get; set; } = string.Empty;

        [Required]
        public string QuoteAsset { get; set; } = string.Empty;

        [Range(0, 10)]
        public int PricePrecision { get; set; }

        [Range(0, 10)]
        public int QuantityPrecision { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinNotional { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MaxQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal StepSize { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TickSize { get; set; }

        public bool IsActive { get; set; } = true;
    }
}