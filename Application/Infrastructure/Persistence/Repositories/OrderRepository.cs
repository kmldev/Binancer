using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BinanceTradingBot.Application.Interfaces;
using BinanceTradingBot.Domain.Enums;
using BinanceTradingBot.Domain.Models;
using BinanceTradingBot.Infrastructure.Persistence.Contexts;

namespace BinanceTradingBot.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository for managing order data
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly TradingDbContext _dbContext;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(
            TradingDbContext dbContext,
            ILogger<OrderRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Saves a new order to the database
        /// </summary>
        public async Task<long> SaveOrderAsync(OrderResult order)
        {
            try
            {
                // Check if order already exists
                var existingOrder = await _dbContext.Orders
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                if (existingOrder != null)
                {
                    _logger.LogWarning("Order with ID {OrderId} already exists", order.Id);
                    return existingOrder.Id;
                }

                // Create new order entity
                var orderEntity = new Order
                {
                    Id = order.Id,
                    Symbol = order.Symbol,
                    Type = order.Type,
                    Side = order.Side,
                    Price = order.Price,
                    Quantity = order.Quantity,
                    ExecutedQuantity = order.ExecutedQuantity,
                    Status = order.Status,
                    CreateTime = order.CreateTime,
                    UpdateTime = order.UpdateTime,
                    StopPrice = order.StopPrice,
                    ClientOrderId = order.ClientOrderId,
                    Commission = order.Commission,
                    CommissionAsset = order.CommissionAsset
                };

                await _dbContext.Orders.AddAsync(orderEntity);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} saved successfully", order.Id);

                return order.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving order {OrderId}", order.Id);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing order in the database
        /// </summary>
        public async Task UpdateOrderAsync(OrderResult order)
        {
            try
            {
                var existingOrder = await _dbContext.Orders
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                if (existingOrder == null)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found for update", order.Id);
                    await SaveOrderAsync(order);
                    return;
                }

                // Update order properties
                existingOrder.Status = order.Status;
                existingOrder.ExecutedQuantity = order.ExecutedQuantity;
                existingOrder.UpdateTime = order.UpdateTime ?? DateTime.UtcNow;
                existingOrder.Commission = order.Commission;
                existingOrder.CommissionAsset = order.CommissionAsset;

                _dbContext.Orders.Update(existingOrder);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} updated successfully", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", order.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets an order by its ID
        /// </summary>
        public async Task<OrderResult> GetOrderByIdAsync(long orderId)
        {
            try
            {
                var order = await _dbContext.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found", orderId);
                    return null;
                }

                return MapToOrderResult(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Gets orders for a specific symbol
        /// </summary>
        public async Task<List<OrderResult>> GetOrdersBySymbolAsync(string symbol, int limit = 50)
        {
            try
            {
                var orders = await _dbContext.Orders
                    .Where(o => o.Symbol == symbol)
                    .OrderByDescending(o => o.CreateTime)
                    .Take(limit)
                    .ToListAsync();

                return orders.Select(MapToOrderResult).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for symbol {Symbol}", symbol);
                throw;
            }
        }

        /// <summary>
        /// Gets currently open orders
        /// </summary>
        public async Task<List<OrderResult>> GetOpenOrdersAsync()
        {
            try
            {
                var openOrders = await _dbContext.Orders
                    .Where(o => o.Status == OrderStatus.New || o.Status == OrderStatus.PartiallyFilled)
                    .OrderByDescending(o => o.CreateTime)
                    .ToListAsync();

                return openOrders.Select(MapToOrderResult).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving open orders");
                throw;
            }
        }

        /// <summary>
        /// Maps an Order entity to an OrderResult model
        /// </summary>
        private OrderResult MapToOrderResult(Order order)
        {
            return new OrderResult
            {
                Id = order.Id,
                Symbol = order.Symbol,
                Type = order.Type,
                Side = order.Side,
                Price = order.Price,
                Quantity = order.Quantity,
                ExecutedQuantity = order.ExecutedQuantity,
                Status = order.Status,
                CreateTime = order.CreateTime,
                UpdateTime = order.UpdateTime,
                StopPrice = order.StopPrice,
                ClientOrderId = order.ClientOrderId,
                Commission = order.Commission,
                CommissionAsset = order.CommissionAsset
            };
        }
    }

    /// <summary>
    /// Entity class for storing orders in the database
    /// </summary>
    public class Order
    {
        public long Id { get; set; }
        public string Symbol { get; set; }
        public OrderType Type { get; set; }
        public OrderSide Side { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal ExecutedQuantity { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public decimal? StopPrice { get; set; }
        public string ClientOrderId { get; set; }
        public decimal? Commission { get; set; }
        public string CommissionAsset { get; set; }
    }
}