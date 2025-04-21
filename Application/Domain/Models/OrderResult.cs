using System;
using BinanceTradingBot.Domain.Enums;

namespace BinanceTradingBot.Domain.Models
{
    public class OrderResult
    {
        public long Id { get; set; }
        public string Symbol { get; set; }
        public OrderType Type { get; set; }
        public OrderSide Side { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal ExecutedQuantity { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public decimal? StopPrice { get; set; }
        public string ClientOrderId { get; set; }
        public decimal? Commission { get; set; }
        public string CommissionAsset { get; set; }
    }
}