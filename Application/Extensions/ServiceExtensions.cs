using Microsoft.Extensions.DependencyInjection;
using BinanceTradingBot.Application.Interfaces;
using BinanceTradingBot.Application.Services;

namespace BinanceTradingBot.Application.Extensions
{
    public static class ServiceExtensions
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IPositionService, PositionService>();
            services.AddScoped<IStrategyService, TradingStrategyService>();
            services.AddScoped<IOrderExecutionService, OrderExecutionService>();
            services.AddScoped<IRiskManagementService, RiskManagementService>();
            services.AddScoped<IBacktestService, BacktestService>();

            // Register strategies
            services.AddScoped<ITradingStrategy, MACrossStrategy>();
            // Add other strategies here as they are implemented
        }
    }
}