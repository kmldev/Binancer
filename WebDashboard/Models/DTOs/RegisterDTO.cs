using System.ComponentModel.DataAnnotations;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    /// <summary>
    /// DTO for user registration request.
    /// </summary>
    public class RegisterDTO
    {
        /// <summary>
        /// Gets or sets the username for registration.
        /// </summary>
        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Le nom d'utilisateur doit contenir entre 3 et 50 caractères")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password for registration.
        /// </summary>
        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address for registration.
        /// </summary>
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the role for the registered user.
        /// </summary>
        [Required(ErrorMessage = "Le rôle est requis")]
        public string Role { get; set; } = "User"; // Default to User role
    }
}