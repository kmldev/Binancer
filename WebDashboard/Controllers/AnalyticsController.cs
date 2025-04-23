using Microsoft.AspNetCore.Mvc;
using BinanceTradingBot.WebDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace BinanceTradingBot.WebDashboard.Controllers
{
    [Authorize(Policy = "UserAccess")] // Apply authorization policy
    public class AnalyticsController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(IDashboardService dashboardService, ILogger<AnalyticsController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        // GET: Analytics
        public async Task<IActionResult> Index(string? symbol, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var performanceStats = await _dashboardService.GetPerformanceStatsAsync(symbol, startDate, endDate);
                return View(performanceStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics data for view.");
                // Optionally return an error view or message
                return View("Error", new Models.ErrorViewModel { Message = "Failed to load analytics data." });
            }
        }

        // You might add API actions here for fetching filtered data via AJAX
        // GET: api/Analytics/PerformanceStats
    }
}