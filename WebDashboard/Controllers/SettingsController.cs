using Microsoft.AspNetCore.Mvc;
using BinanceTradingBot.WebDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Controllers
{
    [Authorize(Policy = "AdminOnly")] // Apply Admin authorization policy
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        // GET: Settings
        public async Task<IActionResult> Index()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                return View(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings for view.");
                // Optionally return an error view or message
                return View("Error", new Models.ErrorViewModel { Message = "Failed to load settings." });
            }
        }

        // You might add actions here for updating settings via POST requests
        // POST: Settings/UpdateAppSettings
        // POST: Settings/UpdateRiskManagementSettings
        // POST: Settings/UpdateApiCredentials
    }
}