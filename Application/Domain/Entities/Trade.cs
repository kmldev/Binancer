using System;
using BinanceTradingBot.Domain.Enums;

namespace BinanceTradingBot.Domain.Entities
{
    public class Trade
    {
        public long Id { get; set; }
        public string Symbol { get; set; }
        public OrderSide Side { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Commission { get; set; }
        public string CommissionAsset { get; set; }
        public DateTime ExecutionTime { get; set; }
        public long OrderId { get; set; }
        public long? PositionId { get; set; }
    }
}