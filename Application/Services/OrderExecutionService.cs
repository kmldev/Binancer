using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BinanceTradingBot.Application.Interfaces;
using BinanceTradingBot.Domain.Enums;
using BinanceTradingBot.Domain.Models;
using BinanceTradingBot.Domain.Entities; // Added missing using directive

namespace BinanceTradingBot.Application.Services
{
    /// <summary>
    /// Service responsable de l'exécution des ordres de trading
    /// </summary>
    public class OrderExecutionService
    {
        private readonly IExchangeService _exchangeService;
        private readonly IOrderRepository _orderRepository;
        private readonly IPositionService _positionService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<OrderExecutionService> _logger;
        private readonly AppSettings _config;

        public OrderExecutionService(
            IExchangeService exchangeService,
            IOrderRepository orderRepository,
            IPositionService positionService,
            INotificationService notificationService,
            ILogger<OrderExecutionService> logger,
            IOptions<AppSettings> config)
        {
            _exchangeService = exchangeService;
            _orderRepository = orderRepository;
            _positionService = positionService;
            _notificationService = notificationService;
            _logger = logger;
            _config = config.Value;
        }

        /// <summary>
        /// Exécute un signal de trading
        /// </summary>
        public async Task<OrderResult> ExecuteSignalAsync(string symbol, TradingSignal signal)
        {
            try
            {
                _logger.LogInformation($"Executing {signal.Action} signal for {symbol} at {signal.Price} with confidence {signal.Confidence}");

                // Vérifier si le signal atteint le seuil de confiance minimal
                if (signal.Confidence < _config.MinConfidenceThreshold)
                {
                    _logger.LogInformation($"Signal confidence {signal.Confidence} is below threshold {_config.MinConfidenceThreshold}, skipping execution");
                    return new OrderResult { Symbol = symbol, Status = OrderStatus.Rejected };
                }

                // Vérifier les positions existantes
                var openPositions = await _positionService.GetOpenPositionsAsync();
                var existingPosition = openPositions.Find(p => p.Symbol == symbol);

                // Déterminer la quantité à trader
                decimal quantity = await CalculateOrderQuantityAsync(symbol, signal.Price);

                // Exécuter l'ordre en fonction du signal
                OrderResult result;

                switch (signal.Action)
                {
                    case SignalAction.Buy:
                        // Si une position existe déjà, ne pas acheter à nouveau
                        if (existingPosition != null && !_config.AllowMultiplePositions)
                        {
                            _logger.LogInformation($"Position already exists for {symbol}, skipping buy order");
                            return new OrderResult { Symbol = symbol, Status = OrderStatus.Rejected };
                        }

                        // Placer l'ordre d'achat
                        result = await _exchangeService.PlaceOrderAsync(symbol, OrderType.Market, OrderSide.Buy, quantity);

                        // Si l'ordre est exécuté, créer une nouvelle position
                        if (result.Status == OrderStatus.Filled || result.Status == OrderStatus.PartiallyFilled)
                        {
                            var position = await _positionService.OpenPositionAsync(
                                symbol,
                                result.Price,
                                result.ExecutedQuantity,
                                PositionType.Long);

                            // Placer les ordres de stop loss et take profit si activés
                            if (_config.UseStopLoss && _config.StopLossPercentage > 0)
                            {
                                decimal stopPrice = result.Price * (1 - (decimal)_config.StopLossPercentage);
                                await _exchangeService.PlaceOrderAsync(symbol, OrderType.StopLoss, OrderSide.Sell, quantity, stopPrice);
                            }

                            if (_config.UseTakeProfit && _config.TakeProfitPercentage > 0)
                            {
                                decimal takePrice = result.Price * (1 + (decimal)_config.TakeProfitPercentage);
                                await _exchangeService.PlaceOrderAsync(symbol, OrderType.TakeProfit, OrderSide.Sell, quantity, takePrice);
                            }
                        }
                        break;

                    case SignalAction.Sell:
                        // Si aucune position n'existe, ne pas vendre
                        if (existingPosition == null)
                        {
                            _logger.LogInformation($"No position exists for {symbol}, skipping sell order");
                            return new OrderResult { Symbol = symbol, Status = OrderStatus.Rejected };
                        }

                        // Placer l'ordre de vente
                        result = await _exchangeService.PlaceOrderAsync(symbol, OrderType.Market, OrderSide.Sell, existingPosition.Quantity);

                        // Si l'ordre est exécuté, fermer la position
                        if (result.Status == OrderStatus.Filled || result.Status == OrderStatus.PartiallyFilled)
                        {
                            await _positionService.ClosePositionAsync(existingPosition.Id, result.Price);
                        }
                        break;

                    default:
                        _logger.LogWarning($"No action specified for signal on {symbol}");
                        return new OrderResult { Symbol = symbol, Status = OrderStatus.Rejected };
                }

                // Enregistrer l'ordre dans la base de données
                await _orderRepository.SaveOrderAsync(result);

                // Envoyer une notification
                await _notificationService.SendOrderExecutionNotificationAsync(symbol, result);

                _logger.LogInformation($"Order executed for {symbol}: {result.Side} at {result.Price}, Status: {result.Status}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing {signal.Action} signal for {symbol}");
                await _notificationService.SendErrorNotificationAsync($"Error executing order for {symbol}: {ex.Message}", ex);
                return new OrderResult { Symbol = symbol, Status = OrderStatus.Rejected };
            }
        }

        /// <summary>
        /// Gère les ordres en cours et les positions ouvertes
        /// </summary>
        public async Task ManageOpenOrdersAndPositionsAsync()
        {
            try
            {
                _logger.LogInformation("Managing open orders and positions");

                // Récupérer les ordres ouverts
                var openOrders = await _orderRepository.GetOpenOrdersAsync();

                foreach (var order in openOrders)
                {
                    // Vérifier le statut actuel de l'ordre
                    var updatedOrder = await _exchangeService.CheckOrderStatusAsync(order.Symbol, order.Id);

                    // Si le statut a changé, mettre à jour en base de données
                    if (updatedOrder.Status != order.Status)
                    {
                        await _orderRepository.UpdateOrderAsync(updatedOrder);

                        // Si l'ordre est maintenant exécuté, mettre à jour la position si nécessaire
                        if (updatedOrder.Status == OrderStatus.Filled || updatedOrder.Status == OrderStatus.PartiallyFilled)
                        {
                            // Pour les ordres de vente qui ferment des positions
                            if (updatedOrder.Side == OrderSide.Sell)
                            {
                                var openPositions = await _positionService.GetOpenPositionsAsync();
                                var position = openPositions.Find(p => p.Symbol == updatedOrder.Symbol);

                                if (position != null)
                                {
                                    await _positionService.ClosePositionAsync(position.Id, updatedOrder.Price);
                                    _logger.LogInformation($"Position closed for {updatedOrder.Symbol} at {updatedOrder.Price}");
                                }
                            }
                        }
                    }
                }

                // Gérer les positions ouvertes
                var currentOpenPositions = await _positionService.GetOpenPositionsAsync();

                foreach (var position in currentOpenPositions)
                {
                    try
                    {
                        // Récupérer le prix actuel
                        var currentPrice = await _exchangeService.GetCurrentPriceAsync(position.Symbol);

                        // Calculer le PnL actuel
                        var pnl = position.CalculatePnl(currentPrice);

                        // Vérifier si le stop loss ou take profit est atteint
                        bool stopLossHit = position.Type == PositionType.Long && currentPrice <= position.StopLoss;
                        bool takeProfitHit = position.Type == PositionType.Long && currentPrice >= position.TakeProfit;

                        if (stopLossHit)
                        {
                            _logger.LogInformation($"Stop loss hit for {position.Symbol} position at {currentPrice}");
                            await ClosePositionWithMarketOrderAsync(position, currentPrice);
                        }
                        else if (takeProfitHit)
                        {
                            _logger.LogInformation($"Take profit hit for {position.Symbol} position at {currentPrice}");
                            await ClosePositionWithMarketOrderAsync(position, currentPrice);
                        }

                        // Mettre à jour la valeur actuelle et le PnL dans les logs
                        _logger.LogDebug($"Position {position.Id} for {position.Symbol}: Entry: {position.EntryPrice}, Current: {currentPrice}, PnL: {pnl}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error managing position {position.Id} for {position.Symbol}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing open orders and positions");
                await _notificationService.SendErrorNotificationAsync("Error managing open orders and positions", ex);
            }
        }

        /// <summary>
        /// Ferme une position avec un ordre au marché
        /// </summary>
        private async Task ClosePositionWithMarketOrderAsync(Domain.Entities.Position position, decimal currentPrice)
        {
            try
            {
                // Placer un ordre de marché pour fermer la position
                var result = await _exchangeService.PlaceOrderAsync(
                    position.Symbol,
                    OrderType.Market,
                    OrderSide.Sell,
                    position.Quantity);

                if (result.Status == OrderStatus.Filled || result.Status == OrderStatus.PartiallyFilled)
                {
                    // Fermer la position dans la base de données
                    await _positionService.ClosePositionAsync(position.Id, result.Price);

                    // Enregistrer l'ordre
                    await _orderRepository.SaveOrderAsync(result);

                    // Envoyer une notification
                    await _notificationService.SendOrderExecutionNotificationAsync(position.Symbol, result);

                    _logger.LogInformation($"Position {position.Id} closed at {result.Price} with result: {result.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error closing position {position.Id} with market order");
                await _notificationService.SendErrorNotificationAsync($"Error closing position for {position.Symbol}", ex);
            }
        }

        /// <summary>
        /// Calcule la quantité à échanger en fonction du solde et des paramètres de risque
        /// </summary>
        private async Task<decimal> CalculateOrderQuantityAsync(string symbol, decimal price)
        {
            try
            {
                // Récupérer la paire de trading
                var pair = _config.TradingPairs.Find(p => p.Symbol == symbol);
                if (pair == null)
                {
                    throw new Exception($"Trading pair {symbol} not found in configuration");
                }

                // Récupérer le solde de l'actif de quote (ex: USDT pour BTC/USDT)
                string quoteAsset = pair.QuoteAsset;
                decimal balance = await _exchangeService.GetBalanceAsync(quoteAsset);

                // Calculer la quantité en fonction du risque par trade
                decimal investmentAmount = balance * (decimal)_config.RiskPerTradePercentage;

                // S'assurer que l'investissement minimum est respecté
                if (investmentAmount < _config.MinOrderAmount)
                {
                    _logger.LogWarning($"Investment amount {investmentAmount} {quoteAsset} is below minimum {_config.MinOrderAmount}, adjusting to minimum");
                    investmentAmount = _config.MinOrderAmount;
                }

                // Calculer la quantité
                decimal quantity = investmentAmount / price;

                // Appliquer la règle de quantité minimale
                if (quantity < pair.MinQuantity)
                {
                    _logger.LogWarning($"Calculated quantity {quantity} is below minimum {pair.MinQuantity} for {symbol}, adjusting to minimum");
                    quantity = pair.MinQuantity;
                }

                // Formater la quantité selon la précision de la paire
                quantity = Math.Floor(quantity * (decimal)Math.Pow(10, pair.QuantityPrecision)) / (decimal)Math.Pow(10, pair.QuantityPrecision);

                _logger.LogInformation($"Calculated order quantity for {symbol}: {quantity} at price {price}");

                return quantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating order quantity for {symbol}");
                throw;
            }
        }
    }
}