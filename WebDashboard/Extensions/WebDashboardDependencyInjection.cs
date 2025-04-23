using Microsoft.Extensions.DependencyInjection;
using BinanceTradingBot.WebDashboard.Services;
using BinanceTradingBot.WebDashboard.Services.Implementation;
using BinanceTradingBot.WebDashboard.Repositories;
using BinanceTradingBot.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using BinanceTradingBot.AppSettings;
using Microsoft.Extensions.Configuration;

namespace BinanceTradingBot.WebDashboard.Extensions
{
    public static class WebDashboardDependencyInjection
    {
        public static IServiceCollection AddWebDashboardServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add controllers with views
            services.AddControllersWithViews();
            services.AddRazorPages();

            // Add SignalR
            services.AddSignalR();

            // Register Web Dashboard specific services
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddScoped<ITradingPairService, TradingPairService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<WebDashboard.Services.IPositionService, WebDashboard.Services.Implementation.PositionService>(); // Resolve ambiguity with Application.IPositionService
            services.AddScoped<WebDashboard.Services.IStrategyService, WebDashboard.Services.Implementation.StrategyService>(); // Resolve ambiguity with Application.IStrategyService

            // Register Web Dashboard specific repositories (if any, otherwise use Application/Infrastructure)
            // services.AddScoped<IWebDashboardRepository, WebDashboardRepository>();

            // Add Authentication
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
            services.AddSingleton(jwtSettings); // Register JwtSettings

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
                };
            });

            // Add Authorization Policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                // Add other policies as needed
            });

            // Add global authorization filter for MVC controllers
            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });


            return services;
        }
    }
}