using System.ComponentModel.DataAnnotations;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    /// <summary>
    /// DTO for changing user password.
    /// </summary>
    public class ChangePasswordDTO
    {
        /// <summary>
        /// Gets or sets the current password.
        /// </summary>
        [Required(ErrorMessage = "Le mot de passe actuel est requis")]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new password.
        /// </summary>
        [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the confirmation of the new password.
        /// </summary>
        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [Compare("NewPassword", ErrorMessage = "Le nouveau mot de passe et sa confirmation ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}