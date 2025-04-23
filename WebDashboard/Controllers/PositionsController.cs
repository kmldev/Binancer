using Microsoft.AspNetCore.Mvc;
using BinanceTradingBot.WebDashboard.Services;
using Microsoft.AspNetCore.Authorization;

namespace BinanceTradingBot.WebDashboard.Controllers
{
    [Authorize(Policy = "UserAccess")] // Apply authorization policy
    public class PositionsController : Controller
    {
        private readonly IPositionService _positionService;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(IPositionService positionService, ILogger<PositionsController> logger)
        {
            _positionService = positionService;
            _logger = logger;
        }

        // GET: Positions
        public async Task<IActionResult> Index()
        {
            try
            {
                // Fetch all positions (or filter as needed, e.g., activeOnly=true)
                var positions = await _positionService.GetPositionsAsync();
                return View(positions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving positions for view.");
                // Optionally return an error view or message
                return View("Error", new Models.ErrorViewModel { Message = "Failed to load positions." });
            }
        }
    }
}