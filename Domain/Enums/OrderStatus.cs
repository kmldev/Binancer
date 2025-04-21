namespace BinanceTradingBot.Domain.Enums
{
    public enum OrderStatus
    {
        New,
        PartiallyFilled,
        Filled,
        Canceled,
        Rejected,
        Expired
    }
}