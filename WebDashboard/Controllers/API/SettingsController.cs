using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BinanceTradingBot.WebDashboard.Services;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<AppSettingsDTO>> GetSettings()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paramètres");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des paramètres");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings(AppSettingsDTO settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _settingsService.UpdateSettingsAsync(settings);
                if (!result.Success)
                {
                    return BadRequest(result.Message);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des paramètres");
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour des paramètres");
            }
        }

        [HttpPut("risk-management")]
        public async Task<IActionResult> UpdateRiskSettings(RiskManagementSettingsDTO settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _settingsService.UpdateRiskManagementSettingsAsync(settings);
                if (!result.Success)
                {
                    return BadRequest(result.Message);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des paramètres de gestion du risque");
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour des paramètres de gestion du risque");
            }
        }

        [HttpPut("api-credentials")]
        public async Task<IActionResult> UpdateApiCredentials(ApiCredentialsDTO credentials)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _settingsService.UpdateApiCredentialsAsync(credentials);
                if (!result.Success)
                {
                    return BadRequest(result.Message);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des informations d'API");
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour des informations d'API");
            }
        }
    }
}