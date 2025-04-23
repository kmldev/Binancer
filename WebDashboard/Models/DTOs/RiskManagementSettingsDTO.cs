using System.ComponentModel.DataAnnotations;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    public class RiskManagementSettingsDTO
    {
        [Range(0, 1, ErrorMessage = "L'exposition maximale du portefeuille doit être entre 0 et 1")]
        public decimal MaxPortfolioExposure { get; set; }

        [Range(0, 1, ErrorMessage = "Le seuil d'exposition critique doit être entre 0 et 1")]
        public decimal CriticalExposureThreshold { get; set; }

        [Range(0, 1, ErrorMessage = "La taille maximale de position doit être entre 0 et 1")]
        public decimal MaxPositionSize { get; set; }

        [Range(0, 1, ErrorMessage = "La volatilité maximale autorisée doit être entre 0 et 1")]
        public decimal MaxAllowedVolatility { get; set; }

        [Range(0, 1, ErrorMessage = "Le seuil de sortie d'urgence doit être entre 0 et 1")]
        public decimal EmergencyExitThreshold { get; set; }

        [Range(1, 365, ErrorMessage = "Le nombre maximum de jours par position doit être entre 1 et 365")]
        public int MaxPositionDays { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "La perte quotidienne maximale doit être positive")]
        public decimal MaxDailyLoss { get; set; }
    }
}