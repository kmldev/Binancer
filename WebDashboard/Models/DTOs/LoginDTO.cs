using System.ComponentModel.DataAnnotations;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    /// <summary>
    /// DTO for user login request.
    /// </summary>
    public class LoginDTO
    {
        /// <summary>
        /// Gets or sets the username for login.
        /// </summary>
        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password for login.
        /// </summary>
        [Required(ErrorMessage = "Le mot de passe est requis")]
        public string Password { get; set; } = string.Empty;
    }
}