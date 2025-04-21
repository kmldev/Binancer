using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BinanceTradingBot.Application.Interfaces;
using BinanceTradingBot.Domain.Enums;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Domain.Models;

namespace BinanceTradingBot.Infrastructure.Exchange
{
    /// <summary>
    /// Implémentation du service d'échange pour l'API Binance
    /// </summary>
    public class BinanceApiService : IExchangeService
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly ILogger<BinanceApiService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public BinanceApiService(
            string apiKey,
            string apiSecret,
            ILogger<BinanceApiService> logger,
            bool useTestnet = false)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _logger = logger;
            _httpClient = new HttpClient();
            _baseUrl = useTestnet
                ? "https://testnet.binance.vision/api"
                : "https://api.binance.com/api";

            _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
        }

        /// <summary>
        /// Récupère les données de bougie pour une paire et un intervalle de temps
        /// </summary>
        public async Task<List<CandlestickData>> GetCandlesticksAsync(string symbol, string interval, int limit = 100)
        {
            try
            {
                var endpoint = $"{_baseUrl}/v3/klines";
                var queryParams = $"symbol={symbol}&interval={interval}&limit={limit}";
                var response = await _httpClient.GetStringAsync($"{endpoint}?{queryParams}");

                var rawCandles = JsonConvert.DeserializeObject<List<List<object>>>(response);
                var candles = new List<CandlestickData>();

                foreach (var candle in rawCandles)
                {
                    candles.Add(new CandlestickData
                    {
                        Symbol = symbol,
                        Interval = interval,
                        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(candle[0])).DateTime,
                        Open = Convert.ToDecimal(candle[1]),
                        High = Convert.ToDecimal(candle[2]),
                        Low = Convert.ToDecimal(candle[3]),
                        Close = Convert.ToDecimal(candle[4]),
                        Volume = Convert.ToDecimal(candle[5]),
                        CloseTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(candle[6])).DateTime,
                        QuoteAssetVolume = Convert.ToDecimal(candle[7]),
                        NumberOfTrades = Convert.ToInt32(candle[8]),
                        TakerBuyBaseAssetVolume = Convert.ToDecimal(candle[9]),
                        TakerBuyQuoteAssetVolume = Convert.ToDecimal(candle[10])
                    });
                }

                return candles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving candlesticks for {symbol} on {interval} interval");
                throw;
            }
        }

        /// <summary>
        /// Récupère le solde d'un actif spécifique
        /// </summary>
        public async Task<decimal> GetBalanceAsync(string asset)
        {
            try
            {
                var endpoint = $"{_baseUrl}/v3/account";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var queryParams = $"timestamp={timestamp}";
                var signature = GenerateSignature(queryParams);

                var response = await _httpClient.GetStringAsync($"{endpoint}?{queryParams}&signature={signature}");
                var accountInfo = JsonConvert.DeserializeObject<dynamic>(response);

                foreach (var balance in accountInfo.balances)
                {
                    if (balance.asset.ToString() == asset)
                    {
                        return Convert.ToDecimal(balance.free) + Convert.ToDecimal(balance.locked);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving balance for {asset}");
                throw;
            }
        }

        /// <summary>
        /// Récupère le prix actuel d'une paire de trading
        /// </summary>
        public async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            try
            {
                var endpoint = $"{_baseUrl}/v3/ticker/price";
                var queryParams = $"symbol={symbol}";
                var response = await _httpClient.GetStringAsync($"{endpoint}?{queryParams}");
                var tickerData = JsonConvert.DeserializeObject<dynamic>(response);

                return Convert.ToDecimal(tickerData.price);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving current price for {symbol}");
                throw;
            }
        }

        /// <summary>
        /// Place un ordre d'achat ou de vente
        /// </summary>
        public async Task<OrderResult> PlaceOrderAsync(string symbol, OrderType type, OrderSide side, decimal quantity, decimal? price = null)
        {
            try
            {
                var endpoint = $"{_baseUrl}/v3/order";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var clientOrderId = $"bot_{Guid.NewGuid().ToString("N")}";

                var queryBuilder = new StringBuilder();
                queryBuilder.Append($"symbol={symbol}");
                queryBuilder.Append($"&side={(side == OrderSide.Buy ? "BUY" : "SELL")}");

                string orderType;
                switch (type)
                {
                    case OrderType.Market:
                        orderType = "MARKET";
                        break;
                    case OrderType.Limit:
                        orderType = "LIMIT";
                        break;
                    case OrderType.StopLoss:
                        orderType = "STOP_LOSS";
                        break;
                    case OrderType.TakeProfit:
                        orderType = "TAKE_PROFIT";
                        break;
                    default:
                        orderType = "MARKET";
                        break;
                }

                queryBuilder.Append($"&type={orderType}");
                queryBuilder.Append($"&quantity={quantity}");

                if (price.HasValue && type != OrderType.Market)
                {
                    queryBuilder.Append($"&price={price.Value}");
                    queryBuilder.Append("&timeInForce=GTC");
                }

                queryBuilder.Append($"&newClientOrderId={clientOrderId}");
                queryBuilder.Append($"&timestamp={timestamp}");

                var queryParams = queryBuilder.ToString();
                var signature = GenerateSignature(queryParams);

                var content = new StringContent($"{queryParams}&signature={signature}", Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error placing order: {responseContent}");
                    throw new Exception($"Error placing order: {responseContent}");
                }

                var orderResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                return new OrderResult
                {
                    Id = Convert.ToInt64(orderResponse.orderId),
                    Symbol = symbol,
                    Side = side,
                    Type = type,
                    Price = price ?? 0,
                    Quantity = quantity,
                    ExecutedQuantity = Convert.ToDecimal(orderResponse.executedQty),
                    Status = ParseOrderStatus(orderResponse.status.ToString()),
                    CreateTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(orderResponse.transactTime)).DateTime,
                    ClientOrderId = orderResponse.clientOrderId.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error placing {side} order for {symbol}");
                throw;
            }
        }

        /// <summary>
        /// Annule un ordre existant
        /// </summary>
        public async Task<bool> CancelOrderAsync(string symbol, long orderId)
        {
            try
            {
                var endpoint = $"{_baseUrl}/v3/order";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var queryParams = $"symbol={symbol}&orderId={orderId}&timestamp={timestamp}";
                var signature = GenerateSignature(queryParams);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri($"{endpoint}?{queryParams}&signature={signature}")
                };

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error cancelling order: {responseContent}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling order {orderId} for {symbol}");
                throw;
            }
        }

        /// <summary>
        /// Vérifie le statut d'un ordre
        /// </summary>
        public async Task<OrderResult> CheckOrderStatusAsync(string symbol, long orderId)
        {
            try
            {
                var endpoint = $"{_baseUrl}/v3/order";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var queryParams = $"symbol={symbol}&orderId={orderId}&timestamp={timestamp}";
                var signature = GenerateSignature(queryParams);

                var response = await _httpClient.GetStringAsync($"{endpoint}?{queryParams}&signature={signature}");
                var orderResponse = JsonConvert.DeserializeObject<dynamic>(response);

                return new OrderResult
                {
                    Id = Convert.ToInt64(orderResponse.orderId),
                    Symbol = symbol,
                    Side = ParseOrderSide(orderResponse.side.ToString()),
                    Type = ParseOrderType(orderResponse.type.ToString()),
                    Price = Convert.ToDecimal(orderResponse.price),
                    Quantity = Convert.ToDecimal(orderResponse.origQty),
                    ExecutedQuantity = Convert.ToDecimal(orderResponse.executedQty),
                    Status = ParseOrderStatus(orderResponse.status.ToString()),
                    CreateTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(orderResponse.time)).DateTime,
                    UpdateTime = orderResponse.updateTime != null ? DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(orderResponse.updateTime)).DateTime : (DateTime?)null,
                    StopPrice = orderResponse.stopPrice != null ? Convert.ToDecimal(orderResponse.stopPrice) : (decimal?)null,
                    ClientOrderId = orderResponse.clientOrderId.ToString(),
                    Commission = orderResponse.commission != null ? Convert.ToDecimal(orderResponse.commission) : (decimal?)null,
                    CommissionAsset = orderResponse.commissionAsset != null ? orderResponse.commissionAsset.ToString() : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking order status for order {orderId} for {symbol}");
                throw;
            }
        }

        /// <summary>
        /// Récupère l'historique des ordres pour une paire spécifique
        /// </summary>
        public async Task<List<OrderResult>> GetOrderHistoryAsync(string symbol, int limit = 50)
        {
            try
            {
                var endpoint = $"{_baseUrl}/v3/history/orders";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var queryParams = $"symbol={symbol}&limit={limit}&timestamp={timestamp}";
                var signature = GenerateSignature(queryParams);

                var response = await _httpClient.GetStringAsync($"{endpoint}?{queryParams}&signature={signature}");
                var orderHistory = JsonConvert.DeserializeObject<List<dynamic>>(response);

                var orders = new List<OrderResult>();
                foreach (var order in orderHistory)
                {
                    orders.Add(new OrderResult
                    {
                        Id = Convert.ToInt64(order.orderId),
                        Symbol = symbol,
                        Side = ParseOrderSide(order.side.ToString()),
                        Type = ParseOrderType(order.type.ToString()),
                        Price = Convert.ToDecimal(order.price),
                        Quantity = Convert.ToDecimal(order.origQty),
                        ExecutedQuantity = Convert.ToDecimal(order.executedQty),
                        Status = ParseOrderStatus(order.status.ToString()),
                        CreateTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(order.time)).DateTime,
                        UpdateTime = order.updateTime != null ? DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(order.updateTime)).DateTime : (DateTime?)null,
                        StopPrice = order.stopPrice != null ? Convert.ToDecimal(order.stopPrice) : (decimal?)null,
                        ClientOrderId = order.clientOrderId.ToString(),
                        Commission = order.commission != null ? Convert.ToDecimal(order.commission) : (decimal?)null,
                        CommissionAsset = order.commissionAsset != null ? order.commissionAsset.ToString() : null
                    });
                }

                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving order history for {symbol}");
                throw;
            }
        }

        private string GenerateSignature(string queryParams)
        {
            var totalParams = $"{queryParams}&timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var keyBytes = Encoding.UTF8.GetBytes(_apiSecret);
            var messageBytes = Encoding.UTF8.GetBytes(totalParams);

            using (var hmacsha256 = new HMACSHA256(keyBytes))
            {
                var hash = hmacsha256.ComputeHash(messageBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private OrderStatus ParseOrderStatus(string status)
        {
            return status switch
            {
                "NEW" => OrderStatus.New,
                "PARTIALLY_FILLED" => OrderStatus.PartiallyFilled,
                "FILLED" => OrderStatus.Filled,
                "CANCELED" => OrderStatus.Canceled,
                "REJECTED" => OrderStatus.Rejected,
                "EXPIRED" => OrderStatus.Expired,
                _ => throw new ArgumentOutOfRangeException(nameof(status), $"Unknown order status: {status}")
            };
        }

        private OrderType ParseOrderType(string type)
        {
            return type switch
            {
                "MARKET" => OrderType.Market,
                "LIMIT" => OrderType.Limit,
                "STOP_LOSS" => OrderType.StopLoss,
                "TAKE_PROFIT" => OrderType.TakeProfit,
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown order type: {type}")
            };
        }

        private OrderSide ParseOrderSide(string side)
        {
            return side switch
            {
                "BUY" => OrderSide.Buy,
                "SELL" => OrderSide.Sell,
                _ => throw new ArgumentOutOfRangeException(nameof(side), $"Unknown order side: {side}")
            };
        }
    }
}