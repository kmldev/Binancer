using Microsoft.AspNetCore.Mvc;
using BinanceTradingBot.WebDashboard.Services;
using Microsoft.AspNetCore.Authorization;

namespace BinanceTradingBot.WebDashboard.Controllers
{
    [Authorize(Policy = "UserAccess")] // Apply authorization policy
    public class TradingPairsController : Controller
    {
        private readonly ITradingPairService _tradingPairService;
        private readonly ILogger<TradingPairsController> _logger;

        public TradingPairsController(ITradingPairService tradingPairService, ILogger<TradingPairsController> logger)
        {
            _tradingPairService = tradingPairService;
            _logger = logger;
        }

        // GET: TradingPairs
        public async Task<IActionResult> Index()
        {
            try
            {
                var tradingPairs = await _tradingPairService.GetAllTradingPairsAsync();
                return View(tradingPairs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trading pairs for view.");
                // Optionally return an error view or message
                return View("Error", new Models.ErrorViewModel { Message = "Failed to load trading pairs." });
            }
        }
    }
}