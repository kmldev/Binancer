using Microsoft.AspNetCore.Mvc;
using BinanceTradingBot.WebDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace BinanceTradingBot.WebDashboard.Controllers
{
    [Authorize(Policy = "UserAccess")] // Apply authorization policy
    public class StrategiesController : Controller
    {
        private readonly IStrategyService _strategyService;
        private readonly ILogger<StrategiesController> _logger;

        public StrategiesController(IStrategyService strategyService, ILogger<StrategiesController> logger)
        {
            _strategyService = strategyService;
            _logger = logger;
        }

        // GET: Strategies
        public async Task<IActionResult> Index()
        {
            try
            {
                var strategies = await _strategyService.GetAvailableStrategiesAsync();
                return View(strategies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving strategies for view.");
                // Optionally return an error view or message
                return View("Error", new Models.ErrorViewModel { Message = "Failed to load strategies." });
            }
        }

        // You might add actions here for viewing/editing strategy parameters
        // GET: Strategies/Parameters/{strategyName}
        // POST: Strategies/UpdateParameters
    }
}