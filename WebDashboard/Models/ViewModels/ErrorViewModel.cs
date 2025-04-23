namespace BinanceTradingBot.WebDashboard.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string Message { get; set; } = "Une erreur s'est produite. Veuillez réessayer.";
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}