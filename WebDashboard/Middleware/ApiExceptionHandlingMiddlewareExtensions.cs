using BinanceTradingBot.WebDashboard.Middleware;
using Microsoft.AspNetCore.Builder;

namespace BinanceTradingBot.WebDashboard.Infrastructure
{
    public static class ApiExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiExceptionHandlingMiddleware>();
        }
    }
}