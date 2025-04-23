using Microsoft.Extensions.Options;
using System.Text.Json;
using BinanceTradingBot.WebDashboard.Models;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting; // Assuming IWebHostEnvironment is here
using System.IO;
using System.Text.Json.Nodes; // Required for JsonObject

namespace BinanceTradingBot.WebDashboard.Services.Implementation
{
    public class SettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<AppSettings> _appSettings;
        private readonly string _settingsFilePath;

        public SettingsService(
            ILogger<SettingsService> logger,
            IConfiguration configuration,
            IOptionsMonitor<AppSettings> appSettings,
            IWebHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _configuration = configuration;
            _appSettings = appSettings;
            _settingsFilePath = Path.Combine(hostEnvironment.ContentRootPath, "appsettings.json");
        }

        public async Task<AppSettingsDTO> GetSettingsAsync()
        {
            try
            {
                // Récupérer les paramètres depuis la configuration
                var settings = _appSettings.CurrentValue;

                return new AppSettingsDTO
                {
                    UseTestnet = settings.UseTestnet,
                    DefaultStrategy = settings.DefaultStrategy,
                    RefreshInterval = settings.RefreshInterval,
                    MinConfidenceThreshold = settings.MinConfidenceThreshold,
                    RiskPerTradePercentage = settings.RiskPerTradePercentage,
                    MinOrderAmount = settings.MinOrderAmount,
                    AllowMultiplePositions = settings.AllowMultiplePositions,
                    UseStopLoss = settings.UseStopLoss,
                    UseTakeProfit = settings.TakeProfit,
                    StopLossPercentage = settings.StopLossPercentage,
                    TakeProfitPercentage = settings.TakeProfitPercentage,
                    UseDynamicStopLoss = settings.UseDynamicStopLoss,
                    RestrictTradingHours = settings.RestrictTradingHours,
                    TradingHoursStart = settings.TradingHoursStart,
                    TradingHoursEnd = settings.TradingHoursEnd,
                    EnableEmailNotifications = settings.EnableEmailNotifications,
                    EnableTelegramNotifications = settings.EnableTelegramNotifications
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paramètres");
                throw;
            }
        }

        public async Task<ServiceResult> UpdateSettingsAsync(AppSettingsDTO settingsDTO)
        {
            try
            {
                // Lire le fichier de configuration existant
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var jsonDocument = JsonDocument.Parse(json);
                var rootElement = jsonDocument.RootElement;

                // Créer une copie modifiable du document JSON
                using var jsonDoc = JsonDocument.Parse(json);
                var jsonObject = new JsonObject();

                // Copier toutes les propriétés existantes
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    jsonObject.Add(property.Name, JsonDocument.Parse(property.Value.GetRawText()).RootElement.Clone());
                }

                // Mettre à jour les propriétés
                jsonObject["UseTestnet"] = JsonValue.Create(settingsDTO.UseTestnet);
                jsonObject["DefaultStrategy"] = JsonValue.Create(settingsDTO.DefaultStrategy);
                jsonObject["RefreshInterval"] = JsonValue.Create(settingsDTO.RefreshInterval);
                jsonObject["MinConfidenceThreshold"] = JsonValue.Create(settingsDTO.MinConfidenceThreshold);
                jsonObject["RiskPerTradePercentage"] = JsonValue.Create(settingsDTO.RiskPerTradePercentage);
                jsonObject["MinOrderAmount"] = JsonValue.Create(settingsDTO.MinOrderAmount);
                jsonObject["AllowMultiplePositions"] = JsonValue.Create(settingsDTO.AllowMultiplePositions);
                jsonObject["UseStopLoss"] = JsonValue.Create(settingsDTO.UseStopLoss);
                jsonObject["UseTakeProfit"] = JsonValue.Create(settingsDTO.TakeProfit);
                jsonObject["StopLossPercentage"] = JsonValue.Create(settingsDTO.StopLossPercentage);
                jsonObject["TakeProfitPercentage"] = JsonValue.Create(settingsDTO.TakeProfitPercentage);
                jsonObject["UseDynamicStopLoss"] = JsonValue.Create(settingsDTO.UseDynamicStopLoss);
                jsonObject["RestrictTradingHours"] = JsonValue.Create(settingsDTO.RestrictTradingHours);
                jsonObject["TradingHoursStart"] = JsonValue.Create(settingsDTO.TradingHoursStart);
                jsonObject["TradingHoursEnd"] = JsonValue.Create(settingsDTO.TradingHoursEnd);
                jsonObject["EnableEmailNotifications"] = JsonValue.Create(settingsDTO.EnableEmailNotifications);
                jsonObject["EnableTelegramNotifications"] = JsonValue.Create(settingsDTO.EnableTelegramNotifications);

                // Écrire dans le fichier
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var updatedJson = JsonSerializer.Serialize(jsonObject, options);
                await File.WriteAllTextAsync(_settingsFilePath, updatedJson);

                return ServiceResult.Ok("Paramètres mis à jour avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des paramètres");
                return ServiceResult.Error($"Erreur lors de la mise à jour des paramètres: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateRiskManagementSettingsAsync(RiskManagementSettingsDTO settings)
        {
            try
            {
                // Lire le fichier de configuration existant
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var jsonDocument = JsonDocument.Parse(json);
                var rootElement = jsonDocument.RootElement;

                // Créer une copie modifiable du document JSON
                using var jsonDoc = JsonDocument.Parse(json);
                var jsonObject = new JsonObject();

                // Copier toutes les propriétés existantes
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    jsonObject.Add(property.Name, JsonDocument.Parse(property.Value.GetRawText()).RootElement.Clone());
                }

                // Mettre à jour les propriétés de gestion du risque
                jsonObject["MaxPortfolioExposure"] = JsonValue.Create(settings.MaxPortfolioExposure);
                jsonObject["CriticalExposureThreshold"] = JsonValue.Create(settings.CriticalExposureThreshold);
                jsonObject["MaxPositionSize"] = JsonValue.Create(settings.MaxPositionSize);
                jsonObject["MaxAllowedVolatility"] = JsonValue.Create(settings.MaxAllowedVolatility);
                jsonObject["EmergencyExitThreshold"] = JsonValue.Create(settings.EmergencyExitThreshold);
                jsonObject["MaxPositionDays"] = JsonValue.Create(settings.MaxPositionDays);
                jsonObject["MaxDailyLoss"] = JsonValue.Create(settings.MaxDailyLoss);

                // Écrire dans le fichier
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var updatedJson = JsonSerializer.Serialize(jsonObject, options);
                await File.WriteAllTextAsync(_settingsFilePath, updatedJson);

                return ServiceResult.Ok("Paramètres de gestion du risque mis à jour avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des paramètres de gestion du risque");
                return ServiceResult.Error($"Erreur lors de la mise à jour des paramètres: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateApiCredentialsAsync(ApiCredentialsDTO credentials)
        {
            try
            {
                // Vérifier que les informations d'API sont valides
                if (string.IsNullOrWhiteSpace(credentials.ApiKey) || string.IsNullOrWhiteSpace(credentials.ApiSecret))
                {
                    return ServiceResult.Error("La clé API et le secret API ne peuvent pas être vides");
                }

                // Lire le fichier de configuration existant
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var jsonDocument = JsonDocument.Parse(json);
                var rootElement = jsonDocument.RootElement;

                // Créer une copie modifiable du document JSON
                using var jsonDoc = JsonDocument.Parse(json);
                var jsonObject = new JsonObject();

                // Copier toutes les propriétés existantes
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    jsonObject.Add(property.Name, JsonDocument.Parse(property.Value.GetRawText()).RootElement.Clone());
                }

                // Mettre à jour les informations d'API
                jsonObject["ApiKey"] = JsonValue.Create(credentials.ApiKey);
                jsonObject["ApiSecret"] = JsonValue.Create(credentials.ApiSecret);
                jsonObject["UseTestnet"] = JsonValue.Create(credentials.UseTestnet);

                // Écrire dans le fichier
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var updatedJson = JsonSerializer.Serialize(jsonObject, options);
                await File.WriteAllTextAsync(_settingsFilePath, updatedJson);

                return ServiceResult.Ok("Informations d'API mises à jour avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des informations d'API");
                return ServiceResult.Error($"Erreur lors de la mise à jour des informations d'API: {ex.Message}");
            }
        }
    }
}