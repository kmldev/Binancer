
namespace BinanceTradingBot.Models
{
    public class TradingSignal
    {
        public SignalAction Action { get; set; }
        public decimal Price { get; set; }
        public double Confidence { get; set; }
    }

    public enum SignalAction
    {
        None,
        Buy,
        Sell
    }
}
