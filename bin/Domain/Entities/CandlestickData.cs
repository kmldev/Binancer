using System;

namespace Domain.Entities
{
    public class CandlestickData
    {
        public int Id { get; set; } // Assuming an Id for entity
        public string Symbol { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal QuoteAssetVolume { get; set; }
        public int NumberOfTrades { get; set; }
        public decimal TakerBuyBaseAssetVolume { get; set; }
        public decimal TakerBuyQuoteAssetVolume { get; set; }
        public bool Ignore { get; set; } // As per Binance API

        // Assuming IsClosed is a calculated property or handled elsewhere for entities
        public bool IsClosed { get; set; }
    }
}