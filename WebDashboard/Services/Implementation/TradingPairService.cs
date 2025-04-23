using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using BinanceTradingBot.WebDashboard.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Services.Implementation
{
    /// <summary>
    /// Implémentation du service pour gérer les paires de trading
    /// </summary>
    public class TradingPairService : ITradingPairService
    {
        private readonly ITradingPairRepository _tradingPairRepository;

        public TradingPairService(ITradingPairRepository tradingPairRepository)
        {
            _tradingPairRepository = tradingPairRepository;
        }

        /// <summary>
        /// Récupère toutes les paires de trading
        /// </summary>
        /// <returns>Liste des DTOs de paire de trading</returns>
        public async Task<IEnumerable<TradingPairDTO>> GetAllTradingPairsAsync()
        {
            var tradingPairs = await _tradingPairRepository.GetAllAsync();
            return tradingPairs.Select(tp => new TradingPairDTO
            {
                Id = tp.Id,
                Symbol = tp.Symbol,
                IsActive = tp.IsActive,
                MinTradeSize = tp.MinTradeSize,
                PricePrecision = tp.PricePrecision,
                QuantityPrecision = tp.QuantityPrecision,
                // Add other relevant properties
            }).ToList();
        }

        /// <summary>
        /// Récupère une paire de trading par son symbole
        /// </summary>
        /// <param name="symbol">Symbole de la paire de trading</param>
        /// <returns>Le DTO de paire de trading ou null si non trouvé</returns>
        public async Task<TradingPairDTO?> GetTradingPairBySymbolAsync(string symbol)
        {
            var tradingPair = await _tradingPairRepository.GetBySymbolAsync(symbol);
            if (tradingPair == null)
            {
                return null;
            }

            return new TradingPairDTO
            {
                Id = tradingPair.Id,
                Symbol = tradingPair.Symbol,
                IsActive = tradingPair.IsActive,
                MinTradeSize = tradingPair.MinTradeSize,
                PricePrecision = tradingPair.PricePrecision,
                QuantityPrecision = tradingPair.QuantityPrecision,
                // Add other relevant properties
            };
        }

        /// <summary>
        /// Crée une nouvelle paire de trading
        /// </summary>
        /// <param name="pairDTO">DTO de la paire de trading à créer</param>
        /// <returns>Le DTO de la paire de trading créée</returns>
        public async Task<TradingPairDTO> CreateTradingPairAsync(TradingPairDTO pairDTO)
        {
            var tradingPair = new TradingPair
            {
                Symbol = pairDTO.Symbol,
                IsActive = pairDTO.IsActive,
                MinTradeSize = pairDTO.MinTradeSize,
                PricePrecision = pairDTO.PricePrecision,
                QuantityPrecision = pairDTO.QuantityPrecision,
                // Map other relevant properties
            };

            await _tradingPairRepository.AddAsync(tradingPair);
            await _tradingPairRepository.SaveChangesAsync();

            pairDTO.Id = tradingPair.Id; // Update DTO with generated ID
            return pairDTO;
        }

        /// <summary>
        /// Met à jour une paire de trading existante
        /// </summary>
        /// <param name="pairDTO">DTO de la paire de trading à mettre à jour</param>
        /// <returns>True si la mise à jour a réussi, false sinon</returns>
        public async Task<bool> UpdateTradingPairAsync(TradingPairDTO pairDTO)
        {
            var tradingPair = await _tradingPairRepository.GetByIdAsync(pairDTO.Id);
            if (tradingPair == null)
            {
                return false;
            }

            tradingPair.Symbol = pairDTO.Symbol;
            tradingPair.IsActive = pairDTO.IsActive;
            tradingPair.MinTradeSize = pairDTO.MinTradeSize;
            tradingPair.PricePrecision = pairDTO.PricePrecision;
            tradingPair.QuantityPrecision = pairDTO.QuantityPrecision;
            // Map other relevant properties

            await _tradingPairRepository.UpdateAsync(tradingPair);
            await _tradingPairRepository.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Bascule l'état actif d'une paire de trading
        /// </summary>
        /// <param name="symbol">Symbole de la paire de trading</param>
        /// <returns>True si l'état a été basculé avec succès, false sinon</returns>
        public async Task<bool> ToggleTradingPairActiveAsync(string symbol)
        {
            return await _tradingPairRepository.ToggleActiveAsync(symbol);
        }
    }
}