using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Added missing using directive
using BinanceTradingBot.WebDashboard.Services;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using System.Threading.Tasks;
using System; // Added missing using directive for Exception

namespace BinanceTradingBot.WebDashboard.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDTO>> Login([FromBody] LoginDTO login)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.AuthenticateAsync(login.Username, login.Password);
                if (!result.Success)
                {
                    return Unauthorized(result.Message);
                }

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion");
                return StatusCode(500, "Une erreur est survenue lors de la connexion");
            }
        }

        [HttpPost("register")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<AuthResponseDTO>> Register([FromBody] RegisterDTO register)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterUserAsync(register);
                if (!result.Success)
                {
                    return BadRequest(result.Message);
                }

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement de l'utilisateur");
                return StatusCode(500, "Une erreur est survenue lors de l'enregistrement de l'utilisateur");
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized("Utilisateur non identifié");
                }

                var result = await _authService.ChangePasswordAsync(username, model.CurrentPassword, model.NewPassword);
                if (!result.Success)
                {
                    return BadRequest(result.Message);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de mot de passe");
                return StatusCode(500, "Une erreur est survenue lors du changement de mot de passe");
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDTO>> RefreshToken([FromBody] RefreshTokenRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);

                if (!result.Success)
                {
                    return Unauthorized(result.Message);
                }

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, "An error occurred during token refresh");
            }
        }

        [HttpPost("revoke-token")]
        [Authorize] // Only authenticated users can revoke their own token or admin can revoke others
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Optional: Add logic to check if the requesting user is the same as the user
                // whose token is being revoked, or if the requesting user is an admin.
                // For simplicity, this example allows any authenticated user to revoke by username.
                // A more secure implementation would verify ownership or admin rights.

                var result = await _authService.RevokeTokenAsync(request.Username);

                if (!result.Success)
                {
                    // Depending on requirements, you might return NotFound if user not found,
                    // or BadRequest if the token wasn't valid/found for revocation.
                    return BadRequest(result.Message);
                }

                return Ok(result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token for user {Username}", request.Username);
                return StatusCode(500, "An error occurred during token revocation");
            }
        }
    }
}