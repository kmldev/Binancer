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

                return ServiceResult<AuthResponseDTO>.Ok(new AuthResponseDTO
                {
                    Token = token,
                    Username = user.UserName ?? string.Empty,
                    Role = role,
                    Expiration = DateTime.UtcNow.AddDays(7)
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
    }
}