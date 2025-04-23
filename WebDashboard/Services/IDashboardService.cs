using BinanceTradingBot.WebDashboard.Models;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardSummaryAsync();
        Task<PerformanceStatsViewModel> GetPerformanceStatsAsync(string? symbol = null, DateTime? startDate = null, DateTime? endDate = null);
    }
}