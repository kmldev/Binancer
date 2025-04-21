namespace BinanceTradingBot.Domain.Entities
{
    public class TradingPair
    {
        public string Symbol { get; set; }
        public string BaseAsset { get; set; }
        public string QuoteAsset { get; set; }
        public int PricePrecision { get; set; }
        public int QuantityPrecision { get; set; }
        public decimal MinNotional { get; set; }
        public decimal MinQuantity { get; set; }
        public decimal MaxQuantity { get; set; }
        public decimal StepSize { get; set; }
        public decimal TickSize { get; set; }
        public bool IsActive { get; set; } = true;
    }
}