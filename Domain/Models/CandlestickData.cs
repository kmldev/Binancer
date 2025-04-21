using System;

namespace Domain.Models
{
    public class CandlestickData
    {
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public bool IsClosed { get; set; }

        // Calculated properties can be added here if needed based on specific requirements
        // Example:
        // public decimal Range => High - Low;
    }
}