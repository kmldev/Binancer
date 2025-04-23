using Microsoft.EntityFrameworkCore;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Domain.Enums;
using BinanceTradingBot.Infrastructure.Persistence.Contexts;
using BinanceTradingBot.WebDashboard.Models;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BinanceTradingBot.Application.Interfaces; // Assuming IExchangeService is here

namespace BinanceTradingBot.WebDashboard.Services.Implementation
{
    public class DashboardService : IDashboardService
    {
        private readonly TradingDbContext _dbContext;
        private readonly ILogger<DashboardService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IExchangeService _exchangeService;

        public DashboardService(TradingDbContext dbContext, ILogger<DashboardService> logger,
            IConfiguration configuration, IExchangeService exchangeService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _configuration = configuration;
            _exchangeService = exchangeService;
        }

        public async Task<DashboardViewModel> GetDashboardSummaryAsync()
        {
            try
            {
                // Récupérer les positions
                var allPositions = await _dbContext.Positions.ToListAsync();
                var openPositions = allPositions.Where(p => p.Status == PositionStatus.Open).ToList();
                var closedPositions = allPositions.Where(p => p.Status == PositionStatus.Closed).ToList();

                // Calculer les profits
                decimal totalProfit = closedPositions.Sum(p => p.Profit ?? 0);

                // Calculer les profits par période
                var now = DateTime.UtcNow;
                var startOfDay = now.Date;
                var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                var startOfMonth = new DateTime(now.Year, now.Month, 1);

                decimal dailyProfit = closedPositions
                    .Where(p => p.CloseTime >= startOfDay)
                    .Sum(p => p.Profit ?? 0);

                decimal weeklyProfit = closedPositions
                    .Where(p => p.CloseTime >= startOfWeek)
                    .Sum(p => p.Profit ?? 0);

                decimal monthlyProfit = closedPositions
                    .Where(p => p.CloseTime >= startOfMonth)
                    .Sum(p => p.Profit ?? 0);

                // Calculer les statistiques de performance
                int winningTrades = closedPositions.Count(p => (p.Profit ?? 0) > 0);
                int losingTrades = closedPositions.Count(p => (p.Profit ?? 0) < 0);
                decimal winRate = closedPositions.Count > 0
                    ? (decimal)winningTrades / closedPositions.Count
                    : 0;

                decimal averageProfit = winningTrades > 0
                    ? closedPositions.Where(p => (p.Profit ?? 0) > 0).Average(p => p.Profit ?? 0)
                    : 0;

                decimal averageLoss = losingTrades > 0
                    ? closedPositions.Where(p => (p.Profit ?? 0) < 0).Average(p => p.Profit ?? 0)
                    : 0;

                decimal totalWinning = closedPositions.Where(p => (p.Profit ?? 0) > 0).Sum(p => p.Profit ?? 0);
                decimal totalLosing = closedPositions.Where(p => (p.Profit ?? 0) < 0).Sum(p => p.Profit ?? 0);
                decimal profitFactor = totalLosing != 0 ? Math.Abs(totalWinning / totalLosing) : 0;

                // Récupérer les dernières positions
                var recentPositions = allPositions
                    .OrderByDescending(p => p.Status == PositionStatus.Open ? DateTime.MaxValue : p.CloseTime)
                    .ThenByDescending(p => p.OpenTime)
                    .Take(10)
                    .Select(p => MapToPositionDTO(p))
                    .ToList();

                // Récupérer les soldes des actifs (simulation pour le moment)
                var assetBalances = new Dictionary<string, decimal>
                {
                    { "USDT", 1000 },
                    { "BTC", 0.05m },
                    { "ETH", 1.2m }
                };

                // Récupérer les prix actuels (simulation pour le moment)
                var currentPrices = new Dictionary<string, decimal>
                {
                    { "BTCUSDT", 60000 },
                    { "ETHUSDT", 3000 }
                };

                // État du bot
                var appSettings = _configuration.GetSection("AppSettings").Get<AppSettings>(); // Assuming AppSettings is configured in config
                bool isRunning = true; // À implémenter selon le statut réel du bot

                return new DashboardViewModel
                {
                    TotalPositions = allPositions.Count,
                    OpenPositions = openPositions.Count,
                    ClosedPositions = closedPositions.Count,
                    TotalProfit = totalProfit,
                    DailyProfit = dailyProfit,
                    WeeklyProfit = weeklyProfit,
                    MonthlyProfit = monthlyProfit,

                    WinRate = winRate,
                    TotalTrades = closedPositions.Count,
                    WinningTrades = winningTrades,
                    LosingTrades = losingTrades,
                    AverageProfit = averageProfit,
                    AverageLoss = averageLoss,
                    ProfitFactor = profitFactor,
                    MaxDrawdown = CalculateMaxDrawdown(closedPositions),

                    RecentPositions = recentPositions,
                    AssetBalances = assetBalances,
                    CurrentPrices = currentPrices,

                    IsRunning = isRunning,
                    LastUpdateTime = DateTime.UtcNow,
                    CurrentStrategy = appSettings?.DefaultStrategy ?? "N/A",
                    ActiveTradingPairs = appSettings?.TradingPairs?.Count(p => p.IsActive) ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du résumé du dashboard");
                throw;
            }
        }

        public async Task<PerformanceStatsViewModel> GetPerformanceStatsAsync(string? symbol = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Définir les dates par défaut si non spécifiées
                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                // Construire la requête pour les positions
                var query = _dbContext.Positions.AsQueryable();

                // Filtrer par symbole si spécifié
                if (!string.IsNullOrEmpty(symbol))
                {
                    query = query.Where(p => p.Symbol == symbol);
                }

                // Filtrer par dates
                query = query.Where(p => p.OpenTime >= startDate &&
                                      (p.Status == PositionStatus.Closed ? p.CloseTime <= endDate : true));

                // Récupérer les positions
                var positions = await query.ToListAsync();
                var closedPositions = positions.Where(p => p.Status == PositionStatus.Closed).ToList();

                // Calculer les statistiques
                decimal totalProfit = closedPositions.Sum(p => p.Profit ?? 0);
                int winningTrades = closedPositions.Count(p => (p.Profit ?? 0) > 0);
                int losingTrades = closedPositions.Count(p => (p.Profit ?? 0) < 0);
                decimal winRate = closedPositions.Count > 0
                    ? (decimal)winningTrades / closedPositions.Count
                    : 0;

                decimal averageProfit = closedPositions.Count > 0
                    ? closedPositions.Average(p => p.Profit ?? 0)
                    : 0;

                decimal maxProfit = closedPositions.Count > 0
                    ? closedPositions.Max(p => p.Profit ?? 0)
                    : 0;

                decimal maxLoss = closedPositions.Count > 0
                    ? closedPositions.Min(p => p.Profit ?? 0)
                    : 0;

                decimal totalWinning = closedPositions.Where(p => (p.Profit ?? 0) > 0).Sum(p => p.Profit ?? 0);
                decimal totalLosing = closedPositions.Where(p => (p.Profit ?? 0) < 0).Sum(p => p.Profit ?? 0);
                decimal profitFactor = totalLosing != 0 && totalLosing != 0 ? Math.Abs(totalWinning / totalLosing) : 0;

                // Calculer la moyenne des périodes de détention
                int averageHoldingPeriodHours = 0;
                if (closedPositions.Count > 0)
                {
                    var totalHours = closedPositions.Sum(p =>
                        p.CloseTime.HasValue
                            ? (int)(p.CloseTime.Value - p.OpenTime).TotalHours
                            : 0);
                    averageHoldingPeriodHours = totalHours / closedPositions.Count;
                }

                // Calculer la courbe d'équité
                var equityCurve = CalculateEquityCurve(closedPositions);

                // Calculer les positions par stratégie
                var positionsByStrategy = closedPositions
                    .GroupBy(p => p.Strategy)
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                    .ToList();

                // Créer et retourner le résultat
                return new PerformanceStatsViewModel
                {
                    Symbol = symbol ?? "ALL",
                    StartDate = startDate.Value,
                    EndDate = endDate.Value,

                    TotalProfit = totalProfit,
                    WinRate = winRate,
                    TotalTrades = closedPositions.Count,
                    WinningTrades = winningTrades,
                    LosingTrades = losingTrades,
                    AverageProfit = averageProfit,
                    MaxProfit = maxProfit,
                    MaxLoss = maxLoss,
                    ProfitFactor = profitFactor,
                    SharpeRatio = CalculateSharpeRatio(closedPositions),
                    MaxDrawdown = CalculateMaxDrawdown(closedPositions),
                    MaxDrawdownPercentage = CalculateMaxDrawdownPercentage(closedPositions),
                    AverageHoldingPeriodHours = averageHoldingPeriodHours,

                    Positions = positions.Select(p => MapToPositionDTO(p)).ToList(),
                    EquityCurve = equityCurve,
                    PositionsByStrategy = positionsByStrategy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de performance");
                throw;
            }
        }

        private decimal CalculateMaxDrawdown(List<Position> positions)
        {
            // Implémentation simplifiée du calcul du drawdown maximal
            if (!positions.Any())
                return 0;

            var sortedPositions = positions
                .Where(p => p.Status == PositionStatus.Closed && p.Profit.HasValue)
                .OrderBy(p => p.CloseTime)
                .ToList();

            if (!sortedPositions.Any())
                return 0;

            decimal peak = 0;
            decimal maxDrawdown = 0;
            decimal currentBalance = 0;

            foreach (var position in sortedPositions)
            {
                currentBalance += position.Profit.Value;

                if (currentBalance > peak)
                {
                    peak = currentBalance;
                }
                else if (peak - currentBalance > maxDrawdown)
                {
                    maxDrawdown = peak - currentBalance;
                }
            }

            return maxDrawdown;
        }

        private decimal CalculateMaxDrawdownPercentage(List<Position> positions)
        {
            // Implémentation simplifiée du calcul du drawdown maximal en pourcentage
            if (!positions.Any())
                return 0;

            var sortedPositions = positions
                .Where(p => p.Status == PositionStatus.Closed && p.Profit.HasValue)
                .OrderBy(p => p.CloseTime)
                .ToList();

            if (!sortedPositions.Any())
                return 0;

            decimal peak = 0;
            decimal maxDrawdownPercentage = 0;
            decimal currentBalance = 0;

            foreach (var position in sortedPositions)
            {
                currentBalance += position.Profit.Value;

                if (currentBalance > peak)
                {
                    peak = currentBalance;
                }
                else if (peak > 0) // Avoid division by zero
                {
                    decimal drawdown = peak - currentBalance;
                    decimal drawdownPercentage = drawdown / peak;
                    if (drawdownPercentage > maxDrawdownPercentage)
                    {
                        maxDrawdownPercentage = drawdownPercentage;
                    }
                }
            }

            return maxDrawdownPercentage;
        }

        private List<KeyValuePair<DateTime, decimal>> CalculateEquityCurve(List<Position> positions)
        {
            // Implémentation simplifiée du calcul de la courbe d'équité
            var equityCurve = new List<KeyValuePair<DateTime, decimal>>();
            decimal currentEquity = 0;

            var sortedPositions = positions
                .Where(p => p.Status == PositionStatus.Closed && p.Profit.HasValue)
                .OrderBy(p => p.CloseTime)
                .ToList();

            equityCurve.Add(new KeyValuePair<DateTime, decimal>(DateTime.MinValue, 0)); // Starting point

            foreach (var position in sortedPositions)
            {
                currentEquity += position.Profit.Value;
                if (position.CloseTime.HasValue)
                {
                    equityCurve.Add(new KeyValuePair<DateTime, decimal>(position.CloseTime.Value, currentEquity));
                }
            }

            return equityCurve;
        }

        private decimal CalculateSharpeRatio(List<Position> positions)
        {
            // Implémentation simplifiée du calcul du ratio de Sharpe
            // Nécessite plus de données (taux sans risque, volatilité) pour un calcul précis
            return 0; // Placeholder
        }

        private PositionDTO MapToPositionDTO(Position position)
        {
            // Assuming you have a way to get the current price for calculating CurrentProfit and CurrentProfitPercentage
            // This is a placeholder implementation
            decimal currentPrice = 0; // Get current price for position.Symbol
            if (_exchangeService != null)
            {
                 // Example: Fetch current price (needs proper implementation)
                 // var priceData = await _exchangeService.GetMarketDataAsync(position.Symbol, TimeFrame.M1);
                 // currentPrice = priceData?.ClosePrice ?? 0;
            }


            decimal currentProfit = 0;
            decimal currentProfitPercentage = 0;

            if (position.Status == PositionStatus.Open && currentPrice > 0)
            {
                 currentProfit = (currentPrice - position.EntryPrice) * position.Quantity * (position.Type == PositionType.Long ? 1 : -1);
                 currentProfitPercentage = (currentPrice - position.EntryPrice) / position.EntryPrice * (position.Type == PositionType.Long ? 100 : -100);
            }


            return new PositionDTO
            {
                Id = position.Id,
                Symbol = position.Symbol,
                Type = position.Type,
                Status = position.Status,
                EntryPrice = position.EntryPrice,
                ExitPrice = position.ExitPrice,
                Quantity = position.Quantity,
                StopLoss = position.StopLoss,
                TakeProfit = position.TakeProfit,
                OpenTime = position.OpenTime,
                CloseTime = position.CloseTime,
                Profit = position.Profit,
                Strategy = position.Strategy,
                CurrentProfit = currentProfit,
                CurrentProfitPercentage = currentProfitPercentage
            };
        }
    }

    // Placeholder for AppSettings class, assuming it exists in your main project or needs to be created
    public class AppSettings
    {
        public string? DefaultStrategy { get; set; }
        public List<TradingPairSetting>? TradingPairs { get; set; }
    }

    public class TradingPairSetting
    {
        public string Symbol { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}