using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Added missing using directive
using BinanceTradingBot.WebDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using System; // Added missing using directive for Exception
using System.Threading.Tasks; // Added missing using directive for Task

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