using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    public class AppSettingsDTO
    {
        [JsonIgnore] // Ne pas exposer ces informations sensibles
        public string? ApiKey { get; set; }

        [JsonIgnore] // Ne pas exposer ces informations sensibles
        public string? ApiSecret { get; set; }

        public bool UseTestnet { get; set; }

        public string? DefaultStrategy { get; set; }

        [Range(1, 3600, ErrorMessage = "L'intervalle de rafraîchissement doit être entre 1 et 3600 secondes")]
        public int RefreshInterval { get; set; }

        [Range(0.1, 1.0, ErrorMessage = "Le seuil de confiance doit être entre 0.1 et 1.0")]
        public double MinConfidenceThreshold { get; set; }

        [Range(0.001, 0.5, ErrorMessage = "Le pourcentage de risque par transaction doit être entre 0.1% et 50%")]
        public double RiskPerTradePercentage { get; set; }

        [Range(1, 10000, ErrorMessage = "Le montant minimum de commande doit être entre 1 et 10000")]
        public decimal MinOrderAmount { get; set; }

        public bool AllowMultiplePositions { get; set; }

        public bool UseStopLoss { get; set; }

        public bool UseTakeProfit { get; set; }

        [Range(0.001, 0.5, ErrorMessage = "Le pourcentage de stop loss doit être entre 0.1% et 50%")]
        public double StopLossPercentage { get; set; }

        [Range(0.001, 1.0, ErrorMessage = "Le pourcentage de take profit doit être entre 0.1% et 100%")]
        public double TakeProfitPercentage { get; set; }

        public bool UseDynamicStopLoss { get; set; }

        public bool RestrictTradingHours { get; set; }

        public string? TradingHoursStart { get; set; }

        public string? TradingHoursEnd { get; set; }

        // Notification settings
        public bool EnableEmailNotifications { get; set; }

        public bool EnableTelegramNotifications { get; set; }
    }
}