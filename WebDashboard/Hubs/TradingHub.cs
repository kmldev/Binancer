using Microsoft.AspNetCore.SignalR;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using Microsoft.Extensions.Logging;

namespace BinanceTradingBot.WebDashboard.Hubs
{
    public class TradingHub : Hub
    {
        private readonly ILogger<TradingHub> _logger;

        public TradingHub(ILogger<TradingHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinSymbolGroup(string symbol)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, symbol);
            _logger.LogInformation("Client {ConnectionId} a rejoint le groupe {Symbol}", Context.ConnectionId, symbol);
            await Clients.Caller.SendAsync("SymbolJoined", symbol);
        }

        public async Task LeaveSymbolGroup(string symbol)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, symbol);
            _logger.LogInformation("Client {ConnectionId} a quitté le groupe {Symbol}", Context.ConnectionId, symbol);
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        // Ces méthodes seront appelées depuis le serveur
        public async Task BroadcastNewTrade(string symbol, PositionDTO position)
        {
            await Clients.Group(symbol).SendAsync("NewTrade", position);
            await Clients.All.SendAsync("NewTrade", position);
        }

        public async Task BroadcastPositionUpdate(string symbol, PositionDTO position)
        {
            await Clients.Group(symbol).SendAsync("PositionUpdate", position);
            await Clients.All.SendAsync("PositionUpdate", position);
        }

        public async Task BroadcastPriceUpdate(string symbol, decimal price)
        {
            await Clients.Group(symbol).SendAsync("PriceUpdate", symbol, price);
            await Clients.All.SendAsync("PriceUpdate", symbol, price);
        }

        public async Task BroadcastBotStatus(bool isRunning)
        {
            await Clients.All.SendAsync("BotStatusUpdate", isRunning);
        }
    }
}