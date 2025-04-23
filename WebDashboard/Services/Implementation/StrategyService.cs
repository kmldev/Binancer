using Microsoft.Extensions.Caching.Memory;
using BinanceTradingBot.Domain.Models;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting; // Assuming IWebHostEnvironment is here
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes; // Required for JsonObject

namespace BinanceTradingBot.WebDashboard.Services.Implementation
{
    public class StrategyService : IStrategyService
    {
        private readonly ILogger<StrategyService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly string _settingsFilePath;

        public StrategyService(
            ILogger<StrategyService> logger,
            IMemoryCache cache,
            IConfiguration configuration,
            IWebHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
            _settingsFilePath = Path.Combine(hostEnvironment.ContentRootPath, "appsettings.json");
        }

        public async Task<IEnumerable<StrategyDTO>> GetAvailableStrategiesAsync()
        {
            try
            {
                const string cacheKey = "AvailableStrategies";

                // Tentative de récupération depuis le cache
                if (_cache.TryGetValue(cacheKey, out IEnumerable<StrategyDTO>? cachedStrategies))
                {
                    return cachedStrategies!;
                }

                // Récupérer les stratégies disponibles
                // Dans un cas réel, il faudrait détecter dynamiquement les stratégies dans l'assembly
                var strategies = new List<StrategyDTO>
                {
                    new StrategyDTO
                    {
                        Name = "TripleConfirmation",
                        Description = "Stratégie utilisant trois indicateurs techniques pour confirmer les signaux",
                        IsActive = true
                    },
                    new StrategyDTO
                    {
                        Name = "SupportResistance",
                        Description = "Stratégie basée sur les niveaux de support et de résistance",
                        IsActive = false
                    },
                    new StrategyDTO
                    {
                        Name = "TrendFollowing",
                        Description = "Stratégie suivant la tendance du marché",
                        IsActive = false
                    },
                    new StrategyDTO
                    {
                        Name = "MACrossover",
                        Description = "Stratégie de croisement des moyennes mobiles",
                        IsActive = false
                    }
                };

                // Mise en cache pour 1 heure
                _cache.Set(cacheKey, strategies, TimeSpan.FromHours(1));

                return strategies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des stratégies disponibles");
                return Enumerable.Empty<StrategyDTO>();
            }
        }

        public async Task<StrategyParametersDTO?> GetStrategyParametersAsync(string strategyName)
        {
            try
            {
                string cacheKey = $"StrategyParameters_{strategyName}";

                // Tentative de récupération depuis le cache
                if (_cache.TryGetValue(cacheKey, out StrategyParametersDTO? cachedParams))
                {
                    return cachedParams;
                }

                // Récupérer les paramètres de stratégie depuis la configuration
                // Dans un cas réel, il faudrait récupérer les paramètres spécifiques à chaque stratégie
                var config = _configuration.GetSection("StrategyParameters");

                var parameters = new StrategyParametersDTO
                {
                    StrategyName = strategyName,
                    RsiPeriod = config.GetValue<int>("RsiPeriod", 14),
                    RsiOversold = config.GetValue<int>("RsiOversold", 30),
                    RsiOverbought = config.GetValue<int>("RsiOverbought", 70),
                    MacdFastPeriod = config.GetValue<int>("MacdFastPeriod", 12),
                    MacdSlowPeriod = config.GetValue<int>("MacdSlowPeriod", 26),
                    MacdSignalPeriod = config.GetValue<int>("MacdSignalPeriod", 9),
                    BbPeriod = config.GetValue<int>("BbPeriod", 20),
                    BbStdDev = config.GetValue<double>("BbStdDev", 2.0),
                    BbWidthThreshold = config.GetValue<double>("BbWidthThreshold", 0.05),
                    CustomParameters = new Dictionary<string, object>()
                };

                // Mise en cache pour 30 minutes
                _cache.Set(cacheKey, parameters, TimeSpan.FromMinutes(30));

                return parameters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paramètres de stratégie {StrategyName}", strategyName);
                return null;
            }
        }

        public async Task<bool> UpdateStrategyParametersAsync(string strategyName, StrategyParametersDTO parameters)
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

                // S'assurer que la section StrategyParameters existe
                if (!jsonObject.ContainsKey("StrategyParameters"))
                {
                    jsonObject.Add("StrategyParameters", new JsonObject());
                }

                // Mettre à jour les paramètres de stratégie
                var strategyParams = jsonObject["StrategyParameters"]!.AsObject();
                strategyParams["RsiPeriod"] = JsonValue.Create(parameters.RsiPeriod);
                strategyParams["RsiOversold"] = JsonValue.Create(parameters.RsiOversold);
                strategyParams["RsiOverbought"] = JsonValue.Create(parameters.RsiOverbought);
                strategyParams["MacdFastPeriod"] = JsonValue.Create(parameters.MacdFastPeriod);
                strategyParams["MacdSlowPeriod"] = JsonValue.Create(parameters.MacdSlowPeriod);
                strategyParams["MacdSignalPeriod"] = JsonValue.Create(parameters.MacdSignalPeriod);
                strategyParams["BbPeriod"] = JsonValue.Create(parameters.BbPeriod);
                strategyParams["BbStdDev"] = JsonValue.Create(parameters.BbStdDev);
                strategyParams["BbWidthThreshold"] = JsonValue.Create(parameters.BbWidthThreshold);

                // Ajouter les paramètres personnalisés
                if (parameters.CustomParameters != null && parameters.CustomParameters.Count > 0)
                {
                    var customParams = new JsonObject();
                    foreach (var param in parameters.CustomParameters)
                    {
                        customParams.Add(param.Key, JsonSerializer.SerializeToElement(param.Value));
                    }
                    strategyParams["CustomParameters"] = customParams;
                }

                // Écrire dans le fichier
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var updatedJson = JsonSerializer.Serialize(jsonObject, options);
                await File.WriteAllTextAsync(_settingsFilePath, updatedJson);

                // Invalider le cache
                _cache.Remove($"StrategyParameters_{strategyName}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des paramètres de stratégie {StrategyName}", strategyName);
                return false;
            }
        }
    }
}