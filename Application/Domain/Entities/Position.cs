using System;
using BinanceTradingBot.Domain.Enums;

namespace BinanceTradingBot.Domain.Entities
{
    public class Position
    {
        public long Id { get; set; }
        public string Symbol { get; set; }
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
        public string Strategy { get; set; }

        public bool IsClosed => Status == PositionStatus.Closed;

        public decimal CalculatePnl(decimal currentPrice)
        {
            if (IsClosed && ExitPrice.HasValue)
            {
                return Profit ?? 0;
            }

            if (Type == PositionType.Long)
            {
                return (currentPrice - EntryPrice) * Quantity;
            }
            else
            {
                return (EntryPrice - currentPrice) * Quantity;
            }
        }
    }
}