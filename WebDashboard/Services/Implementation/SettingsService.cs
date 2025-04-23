using BinanceTradingBot.WebDashboard.Models;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using BinanceTradingBot.WebDashboard.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Services.Implementation
{
    /// <summary>
    /// Implémentation du service pour gérer les paramètres de l'application
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IDistributedCache _cache;
        private const string SettingsCacheKey = "AppSettings";

        public SettingsService(ISettingsRepository settingsRepository, IDistributedCache cache)
        {
            _settingsRepository = settingsRepository;
            _cache = cache;
        }

        /// <summary>
        /// Récupère les paramètres de l'application
        /// </summary>
        /// <returns>DTO des paramètres de l'application</returns>
        public async Task<AppSettingsDTO> GetSettingsAsync()
        {
            var cachedSettings = await _cache.GetAsync(SettingsCacheKey);
            if (cachedSettings != null)
            {
                var json = Encoding.UTF8.GetString(cachedSettings);
                return JsonConvert.DeserializeObject<AppSettingsDTO>(json);
            }

            var settings = await _settingsRepository.GetAllSettingsAsync();
            
            // Map dictionary to AppSettingsDTO
            var appSettings = new AppSettingsDTO();
            // Assuming keys in dictionary match property names in AppSettingsDTO
            // This mapping needs to be more robust, potentially using reflection or a dedicated mapper
            // For simplicity, direct assignment for known keys:
            if (settings.TryGetValue("BinanceApiKey", out var apiKey))
            {
                appSettings.BinanceApiKey = apiKey;
            }
            if (settings.TryGetValue("BinanceApiSecret", out var apiSecret))
            {
                appSettings.BinanceApiSecret = apiSecret;
            }
            // Add other settings properties as needed

            // Cache the settings
            var jsonToCache = JsonConvert.SerializeObject(appSettings);
            await _cache.SetAsync(SettingsCacheKey, Encoding.UTF8.GetBytes(jsonToCache), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) // Cache for 30 minutes
            });

            return appSettings;
        }

        /// <summary>
        /// Met à jour les paramètres de l'application
        /// </summary>
        /// <param name="settings">DTO des paramètres de l'application</param>
        /// <returns>Résultat du service</returns>
        public async Task<ServiceResult> UpdateSettingsAsync(AppSettingsDTO settings)
        {
            // Map AppSettingsDTO to dictionary
            var settingsDictionary = new Dictionary<string, string>();
            // Assuming property names in AppSettingsDTO match keys in dictionary
            // This mapping needs to be more robust
            if (!string.IsNullOrEmpty(settings.BinanceApiKey))
            {
                settingsDictionary["BinanceApiKey"] = settings.BinanceApiKey;
            }
            if (!string.IsNullOrEmpty(settings.BinanceApiSecret))
            {
                settingsDictionary["BinanceApiSecret"] = settings.BinanceApiSecret;
            }
            // Add other settings properties as needed

            await _settingsRepository.UpdateSettingsAsync(settingsDictionary);
            
            // Invalidate the cache
            await _cache.RemoveAsync(SettingsCacheKey);

            return ServiceResult.Success();
        }

        /// <summary>
        /// Met à jour les paramètres de gestion des risques
        /// </summary>
        /// <param name="settings">DTO des paramètres de gestion des risques</param>
        /// <returns>Résultat du service</returns>
        public async Task<ServiceResult> UpdateRiskManagementSettingsAsync(RiskManagementSettingsDTO settings)
        {
            var settingsDictionary = new Dictionary<string, string>
            {
                { "MaxDrawdown", settings.MaxDrawdown.ToString() },
                { "MaxPositionSize", settings.MaxPositionSize.ToString() },
                // Add other risk management settings
            };

            await _settingsRepository.UpdateSettingsAsync(settingsDictionary);
            
            // Invalidate the cache
            await _cache.RemoveAsync(SettingsCacheKey);

            return ServiceResult.Success();
        }

        /// <summary>
        /// Met à jour les identifiants API
        /// </summary>
        /// <param name="credentials">DTO des identifiants API</param>
        /// <returns>Résultat du service</returns>
        public async Task<ServiceResult> UpdateApiCredentialsAsync(ApiCredentialsDTO credentials)
        {
            var settingsDictionary = new Dictionary<string, string>
            {
                { "BinanceApiKey", credentials.ApiKey },
                { "BinanceApiSecret", credentials.ApiSecret }
            };

            await _settingsRepository.UpdateSettingsAsync(settingsDictionary);
            
            // Invalidate the cache
            await _cache.RemoveAsync(SettingsCacheKey);

            return ServiceResult.Success();
        }
    }
}