using System.ComponentModel.DataAnnotations;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    public class ApiCredentialsDTO
    {
        [Required(ErrorMessage = "La clé API est requise")]
        public string ApiKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le secret API est requis")]
        public string ApiSecret { get; set; } = string.Empty;

        public bool UseTestnet { get; set; }
    }
}