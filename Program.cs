using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using BinanceTradingBot.Application.Interfaces;
using BinanceTradingBot.Application.Services;
using BinanceTradingBot.Domain.Models;
using BinanceTradingBot.Infrastructure.Exchange;
using BinanceTradingBot.Infrastructure.Notifications;
using BinanceTradingBot.Infrastructure.Persistence.Contexts;
using BinanceTradingBot.Infrastructure.Persistence.Repositories;

namespace BinanceTradingBot
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                // Setup configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                // Configure Serilog
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .Enrich.FromLogContext()
                    .CreateLogger();

                Log.Information("Starting Binance Trading Bot");

                // Create host with dependency injection
                using var host = CreateHostBuilder(args).Build();
                using var scope = host.Services.CreateScope();

                // Initialize the database
                var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
                await dbContext.Database.MigrateAsync();

                // Get services
                var strategyService = scope.ServiceProvider.GetRequiredService<IStrategyService>();
                var orderExecutionService = scope.ServiceProvider.GetRequiredService<OrderExecutionService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var settings = config.Get<AppSettings>();

                // Run the trading bot
                await RunTradingBotAsync(settings, strategyService, orderExecutionService, logger);

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;

                    // Register configuration
                    services.Configure<AppSettings>(configuration);

                    // Register database
                    services.AddDbContext<TradingDbContext>(options =>
                        options.UseSqlite(configuration.GetValue<string>("DbConnectionString") ?? "Data Source=tradingbot.db"));

                    // Register memory cache
                    services.AddMemoryCache();

                    // Register repositories
                    services.AddScoped<IMarketDataRepository, MarketDataRepository>();
                    services.AddScoped<IOrderRepository, OrderRepository>();

                    // Register services
                    services.AddSingleton<IExchangeService>(provider =>
                    {
                        var config = provider.GetRequiredService<IConfiguration>();
                        var logger = provider.GetRequiredService<ILogger<BinanceApiService>>();
                        return new BinanceApiService(
                            config.GetValue<string>("ApiKey"),
                            config.GetValue<string>("ApiSecret"),
                            logger,
                            config.GetValue<bool>("UseTestnet"));
                    });

                    services.AddScoped<IStrategyService, TradingStrategyService>();
                    services.AddScoped<IPositionService, PositionService>();
                    services.AddScoped<INotificationService, NotificationService>();
                    services.AddScoped<OrderExecutionService>();
                });

        static async Task RunTradingBotAsync(
            AppSettings settings,
            IStrategyService strategyService,
            OrderExecutionService orderExecutionService,
            ILogger<Program> logger)
        {
            logger.LogInformation("Starting trading strategy: {Strategy}", settings.DefaultStrategy);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                logger.LogInformation("Cancellation requested");
                cts.Cancel();
                e.Cancel = true;
            };

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    foreach (var pair in settings.TradingPairs)
                    {
                        if (!pair.IsActive)
                        {
                            continue;
                        }

                        try
                        {
                            logger.LogDebug("Analyzing {Symbol}", pair.Symbol);

                            // Generate trading signal
                            var signal = await strategyService.GenerateSignalAsync(pair.Symbol, "15m");

                            // Execute if there is an actionable signal
                            if (signal.Action != Domain.Enums.SignalAction.None)
                            {
                                logger.LogInformation("Generated {Action} signal for {Symbol} at {Price} with confidence {Confidence}",
                                    signal.Action, pair.Symbol, signal.Price, signal.Confidence);

                                var result = await orderExecutionService.ExecuteSignalAsync(pair.Symbol, signal);

                                logger.LogInformation("Order execution result: {Status} for {Symbol}",
                                    result.Status, pair.Symbol);
                            }
                            else
                            {
                                logger.LogDebug("No signal for {Symbol}", pair.Symbol);
                            }

                            // Manage existing positions and orders
                            await orderExecutionService.ManageOpenOrdersAndPositionsAsync();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error processing {Symbol}", pair.Symbol);
                        }
                    }

                    // Wait for the next interval
                    await Task.Delay(TimeSpan.FromSeconds(settings.RefreshInterval), cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Trading bot stopped");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Critical error in trading bot");
                throw;
            }
        }
    }
}
