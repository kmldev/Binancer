using System.ComponentModel.DataAnnotations;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    /// <summary>
    /// DTO for revoke token request
    /// </summary>
    public class RevokeTokenRequestDTO
    {
        [Required]
        public string Username { get; set; } = string.Empty;
    }
}