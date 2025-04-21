using System;
using Microsoft.EntityFrameworkCore;
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.Infrastructure.Persistence.Repositories;

namespace BinanceTradingBot.Infrastructure.Persistence.Contexts
{
    /// <summary>
    /// Database context for trading data persistence
    /// </summary>
    public class TradingDbContext : DbContext
    {
        public DbSet<CandlestickData> MarketData { get; set; } = null!;
        public DbSet<Trade> Trades { get; set; } = null!;
        public DbSet<Position> Positions { get; set; } = null!;
        public DbSet<TradingPair> TradingPairs { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;

        public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MarketData configuration
            modelBuilder.Entity<CandlestickData>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<CandlestickData>()
                .ToTable("CandlestickData");

            modelBuilder.Entity<CandlestickData>()
                .HasIndex(m => new { m.Symbol, m.Interval, m.Timestamp })
                .IsUnique();

            // Trade configuration
            modelBuilder.Entity<Trade>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<Trade>()
                .HasIndex(t => t.OrderId);

            modelBuilder.Entity<Trade>()
                .HasIndex(t => t.PositionId);

            // Position configuration
            modelBuilder.Entity<Position>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<Position>()
                .HasIndex(p => new { p.Symbol, p.Status });

            // TradingPair configuration
            modelBuilder.Entity<TradingPair>()
                .HasKey(p => p.Symbol);
                
            // Order configuration
            modelBuilder.Entity<Order>()
                .HasKey(o => o.Id);
                
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Symbol);
                
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);
        }
    }
}