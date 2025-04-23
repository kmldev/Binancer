using Microsoft.AspNetCore.Identity;
using System;

namespace BinanceTradingBot.WebDashboard.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        // Ajoutez des propriétés supplémentaires si nécessaire
    }
}