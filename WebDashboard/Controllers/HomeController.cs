using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Added missing using directive
using BinanceTradingBot.WebDashboard.Models;
using BinanceTradingBot.WebDashboard.Services;
using System.Diagnostics;
using System; // Added missing using directive for Exception
using System.Threading.Tasks; // Added missing using directive for Task

namespace BinanceTradingBot.WebDashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IDashboardService dashboardService, ILogger<HomeController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var model = await _dashboardService.GetDashboardSummaryAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du dashboard");
                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Erreur lors du chargement du dashboard"
                });
            }
        }

        [Authorize(Policy = "UserAccess")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}