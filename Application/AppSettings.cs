using System;
using System.Collections.Generic;
using BinanceTradingBot.Domain.Entities;

namespace BinanceTradingBot
{
    public class AppSettings
    {
        // API Binance
        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }
        public bool UseTestnet { get; set; } = true;
        
        // Trading configuration
        public List<TradingPair> TradingPairs { get; set; } = new List<TradingPair>();
        public double RiskPerTradePercentage { get; set; } = 0.02;
        public decimal MinOrderAmount { get; set; } = 10;
        public bool AllowMultiplePositions { get; set; } = false;
        public int RefreshInterval { get; set; } = 60;
        public double MinConfidenceThreshold { get; set; } = 0.7;
        
        // Strategy configuration
        public string? DefaultStrategy { get; set; } = "TripleConfirmation";
        public bool UseStopLoss { get; set; } = true;
        public bool UseTakeProfit { get; set; } = true;
        public double StopLossPercentage { get; set; } = 0.02;
        public double TakeProfitPercentage { get; set; } = 0.05;
        public bool UseDynamicStopLoss { get; set; } = true;
        
        // Risk management
        public decimal MaxPortfolioExposure { get; set; } = 0.8m;
        public decimal CriticalExposureThreshold { get; set; } = 0.9m;
        public decimal MaxPositionSize { get; set; } = 0.2m;
        public decimal MaxAllowedVolatility { get; set; } = 0.05m;
        public decimal EmergencyExitThreshold { get; set; } = 0.1m;
        public int MaxPositionDays { get; set; } = 7;
        public decimal MaxDailyLoss { get; set; } = 100m;
        
        // Trading hours
        public bool RestrictTradingHours { get; set; } = false;
        public string? TradingHoursStart { get; set; } = "00:00:00";
        public string? TradingHoursEnd { get; set; } = "23:59:59";
        
        // Database
        public string? DbConnectionString { get; set; }
        public string CacheExpirationMinutes { get; set; } = "60";
        
        // Notifications
        public bool EnableEmailNotifications { get; set; } = false;
        public string? EmailApiKey { get; set; }
        public string? EmailSender { get; set; }
        public string? EmailRecipient { get; set; }
        
        public bool EnableTelegramNotifications { get; set; } = false;
        public string? TelegramBotToken { get; set; }
        public string? TelegramChatId { get; set; }
        
        // Logging
        public string LogLevel { get; set; } = "Information";
        public bool EnableFileLogging { get; set; } = true;
        public string LogFilePath { get; set; } = "logs/trading_bot.log";
    }
}