using System;
using System.Collections.Generic;
using BinanceTradingBot.Domain.Enums;

namespace BinanceTradingBot.Domain.Models
{
    public class TradingSignal
    {
        public string Symbol { get; set; }
        public SignalAction Action { get; set; }
        public decimal Price { get; set; }
        public double Confidence { get; set; }
        public DateTime Timestamp { get; set; }
        public string Interval { get; set; }
        public string Strategy { get; set; }
        public Dictionary<string, object> Indicators { get; set; } = new Dictionary<string, object>();

        public TradingSignal()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}