using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BinanceTradingBot.WebDashboard.Models;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using BinanceTradingBot.WebDashboard.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity; // Required for UserManager
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BinanceTradingBot.WebDashboard.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ServiceResult<AuthResponseDTO>> AuthenticateAsync(string username, string password)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(username);

                if (user == null)
                {
                    return ServiceResult<AuthResponseDTO>.Error("Nom d'utilisateur ou mot de passe incorrect");
                }

                var result = await _userManager.CheckPasswordAsync(user, password);

                if (!result)
                {
                    return ServiceResult<AuthResponseDTO>.Error("Nom d'utilisateur ou mot de passe incorrect");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";

                var token = GenerateJwtToken(user, role);
                var refreshToken = GenerateRefreshToken();
                var refreshTokenExpiryTime = DateTime.UtcNow.AddDays(14); // Refresh token valid for 14 days

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = refreshTokenExpiryTime;
                await _userManager.UpdateAsync(user);

                return ServiceResult<AuthResponseDTO>.Ok(new AuthResponseDTO
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Username = user.UserName ?? string.Empty,
                    Role = role,
                    Expiration = DateTime.UtcNow.AddDays(7) // Access token valid for 7 days
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'authentification de l'utilisateur {Username}", username);
                return ServiceResult<AuthResponseDTO>.Error("Une erreur s'est produite lors de l'authentification");
            }
        }

        public async Task<ServiceResult<AuthResponseDTO>> RegisterUserAsync(RegisterDTO register)
        {
            try
            {
                var existingUser = await _userManager.FindByNameAsync(register.Username);

                if (existingUser != null)
                {
                    return ServiceResult<AuthResponseDTO>.Error("Le nom d'utilisateur est déjà utilisé");
                }

                existingUser = await _userManager.FindByEmailAsync(register.Email);

                if (existingUser != null)
                {
                    return ServiceResult<AuthResponseDTO>.Error("L'adresse e-mail est déjà utilisée");
                }

                var user = new ApplicationUser
                {
                    UserName = register.Username,
                    Email = register.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, register.Password);

                if (!result.Succeeded)
                {
                    return ServiceResult<AuthResponseDTO>.Error(
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                // Assigner le rôle
                var roleResult = await _userManager.AddToRoleAsync(user, register.Role);

                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Impossible d'assigner le rôle {Role} à l'utilisateur {Username}",
                        register.Role, register.Username);
                }

                var token = GenerateJwtToken(user, register.Role);

                return ServiceResult<AuthResponseDTO>.Ok(new AuthResponseDTO
                {
                    Token = token,
                    Username = user.UserName ?? string.Empty,
                    Role = register.Role,
                    Expiration = DateTime.UtcNow.AddDays(7)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement de l'utilisateur {Username}", register.Username);
                return ServiceResult<AuthResponseDTO>.Error("Une erreur s'est produite lors de l'enregistrement");
            }
        }

        public async Task<ServiceResult<AuthResponseDTO>> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            try
            {
                var principal = GetPrincipalFromExpiredToken(accessToken);
                var username = principal?.Identity?.Name;

                if (username == null)
                {
                    return ServiceResult<AuthResponseDTO>.Error("Invalid access token");
                }

                var user = await _userManager.FindByNameAsync(username);

                if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    return ServiceResult<AuthResponseDTO>.Error("Invalid refresh token");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";

                var newAccessToken = GenerateJwtToken(user, role);
                var newRefreshToken = GenerateRefreshToken();
                var newRefreshTokenExpiryTime = DateTime.UtcNow.AddDays(14);

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = newRefreshTokenExpiryTime;
                await _userManager.UpdateAsync(user);

                return ServiceResult<AuthResponseDTO>.Ok(new AuthResponseDTO
                {
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Username = user.UserName ?? string.Empty,
                    Role = role,
                    Expiration = DateTime.UtcNow.AddDays(7)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token for user {Username}", principal?.Identity?.Name);
                return ServiceResult<AuthResponseDTO>.Error("An error occurred during token refresh");
            }
        }

        public async Task<ServiceResult> ChangePasswordAsync(string username, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(username);

                if (user == null)
                {
                    return ServiceResult.Error("Utilisateur non trouvé");
                }

                var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

                if (!result.Succeeded)
                {
                    return ServiceResult.Error(
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                return ServiceResult.Ok("Mot de passe modifié avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de mot de passe pour l'utilisateur {Username}", username);
                return ServiceResult.Error("Une erreur s'est produite lors du changement de mot de passe");
            }
        }

        /// <summary>
        /// Revokes the refresh token for a user.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>A ServiceResult indicating the outcome of the operation.</returns>
        public async Task<ServiceResult> RevokeTokenAsync(string username)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(username);

                if (user == null)
                {
                    return ServiceResult.Error("User not found.");
                }

                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1); // Set expiry to a past date

                await _userManager.UpdateAsync(user);

                return ServiceResult.Ok("Refresh token revoked successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token for user {Username}", username);
                return ServiceResult.Error("An error occurred during token revocation.");
            }
        }

        private string GenerateJwtToken(ApplicationUser user, string role)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("La clé secrète JWT n'est pas configurée");
            var key = Encoding.ASCII.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, // We don't validate the audience in the expired token
                ValidateIssuer = false, // We don't validate the issuer in the expired token
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"])),
                ValidateLifetime = false // Here we are saying that the token is not expired
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }
}