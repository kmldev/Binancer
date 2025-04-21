using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using BinanceTradingBot.Application.Interfaces;
using BinanceTradingBot.Domain.Models;
using BinanceTradingBot.Domain.Enums; // Added missing using directive

namespace BinanceTradingBot.Infrastructure.Notifications
{
    /// <summary>
    /// Service responsable de l'envoi de notifications via différents canaux
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly AppSettings _config;
        private readonly HttpClient _httpClient;

        public NotificationService(
            ILogger<NotificationService> logger,
            IOptions<AppSettings> config)
        {
            _logger = logger;
            _config = config.Value;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Envoie une notification de signal de trading
        /// </summary>
        public async Task SendTradingSignalNotificationAsync(string symbol, TradingSignal signal)
        {
            try
            {
                string message = $"🔔 SIGNAL DÉTECTÉ: {signal.Action} {symbol} à {signal.Price} (confiance: {signal.Confidence:P0})\n" +
                                 $"Stratégie: {signal.Strategy}\n" +
                                 $"Horodatage: {signal.Timestamp:yyyy-MM-dd HH:mm:ss}";

                // Ajouter des détails sur les indicateurs
                if (signal.Indicators.Count > 0)
                {
                    message += "\nIndicateurs:";
                    foreach (var indicator in signal.Indicators)
                    {
                        message += $"\n- {indicator.Key}: {indicator.Value}";
                    }
                }

                await SendNotificationAsync("Signal de Trading", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending trading signal notification");
            }
        }

        /// <summary>
        /// Envoie une notification d'exécution d'ordre
        /// </summary>
        public async Task SendOrderExecutionNotificationAsync(string symbol, OrderResult order)
        {
            try
            {
                string action = order.Side.ToString().ToUpper();
                string emoji = order.Side == OrderSide.Buy ? "🟢" : "🔴";

                string message = $"{emoji} ORDRE EXÉCUTÉ: {action} {symbol}\n" +
                                 $"Prix: {order.Price}\n" +
                                 $"Quantité: {order.ExecutedQuantity}\n" +
                                 $"Statut: {order.Status}\n" +
                                 $"Horodatage: {order.CreateTime:yyyy-MM-dd HH:mm:ss}";

                await SendNotificationAsync("Exécution d'Ordre", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order execution notification");
            }
        }

        /// <summary>
        /// Envoie une notification d'erreur
        /// </summary>
        public async Task SendErrorNotificationAsync(string errorMessage, Exception? exception = null)
        {
            try
            {
                string message = $"⚠️ ERREUR: {errorMessage}";

                if (exception != null)
                {
                    message += $"\nType: {exception.GetType().Name}";
                    message += $"\nMessage: {exception.Message}";
                    message += $"\nHorodatage: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                }

                await SendNotificationAsync("Erreur Bot Trading", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending error notification");
            }
        }

        /// <summary>
        /// Envoie une notification via tous les canaux configurés
        /// </summary>
        private async Task SendNotificationAsync(string title, string message)
        {
            var tasks = new Task[3];

            // Email via Brevo SMTP API
            if (_config.EnableEmailNotifications)
            {
                tasks[0] = SendEmailAsync(title, message);
            }
            else
            {
                tasks[0] = Task.CompletedTask;
            }

            // Telegram
            if (_config.EnableTelegramNotifications)
            {
                tasks[1] = SendTelegramAsync(message);
            }
            else
            {
                tasks[1] = Task.CompletedTask;
            }

            // Discord (exemple)
            tasks[2] = Task.CompletedTask;

            // Attendre la fin de toutes les tâches de notification
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Envoie un email via Brevo SMTP API
        /// </summary>
        private async Task SendEmailAsync(string subject, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(_config.EmailApiKey) ||
                    string.IsNullOrEmpty(_config.EmailSender) ||
                    string.IsNullOrEmpty(_config.EmailRecipient))
                {
                    _logger.LogWarning("Email notification configuration is incomplete for Brevo");
                    return;
                }

                // Brevo SMTP API endpoint
                var endpoint = "https://api.brevo.com/v3/smtp/email";

                var emailContent = new
                {
                    sender = new { email = _config.EmailSender },
                    to = new[]
                    {
                        new { email = _config.EmailRecipient }
                    },
                    subject = subject,
                    textContent = message // Use textContent for plain text email
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(emailContent),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("api-key", _config.EmailApiKey); // Brevo uses 'api-key' header

                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error sending email via Brevo: {response.StatusCode} - {responseContent}");
                }
                else
                {
                    _logger.LogInformation("Email notification sent successfully via Brevo");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notification via Brevo");
            }
        }

        /// <summary>
        /// Envoie un message via Telegram Bot API
        /// </summary>
        private async Task SendTelegramAsync(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(_config.TelegramBotToken) ||
                    string.IsNullOrEmpty(_config.TelegramChatId))
                {
                    _logger.LogWarning("Telegram notification configuration is incomplete");
                    return;
                }

                var endpoint = $"https://api.telegram.org/bot{_config.TelegramBotToken}/sendMessage";

                var telegramMessage = new
                {
                    chat_id = _config.TelegramChatId,
                    text = message,
                    parse_mode = "Markdown"
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(telegramMessage),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();

                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error sending Telegram message: {responseContent}");
                }
                else
                {
                    _logger.LogInformation("Telegram notification sent successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Telegram notification");
            }
        }
    }
}