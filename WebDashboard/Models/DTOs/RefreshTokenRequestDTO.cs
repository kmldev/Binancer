using System.ComponentModel.DataAnnotations;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    /// <summary>
    /// DTO for refresh token request
    /// </summary>
    public class RefreshTokenRequestDTO
    {
        [Required]
        public string AccessToken { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}