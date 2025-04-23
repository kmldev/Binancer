using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.WebDashboard.Models;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using BinanceTradingBot.WebDashboard.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Services.Implementation
{
    /// <summary>
    /// Implémentation du service pour gérer les positions de trading
    /// </summary>
    public class PositionService : IPositionService
    {
        private readonly IPositionRepository _positionRepository;

        public PositionService(IPositionRepository positionRepository)
        {
            _positionRepository = positionRepository;
        }

        /// <summary>
        /// Récupère les positions de trading
        /// </summary>
        /// <param name="activeOnly">Indique si seules les positions actives doivent être récupérées</param>
        /// <returns>Liste des DTOs de position</returns>
        public async Task<IEnumerable<PositionDTO>> GetPositionsAsync(bool activeOnly = false)
        {
            IEnumerable<Position> positions;
            if (activeOnly)
            {
                positions = await _positionRepository.GetActivePositionsAsync();
            }
            else
            {
                positions = await _positionRepository.GetAllAsync();
            }

            return positions.Select(p => new PositionDTO
            {
                Id = p.Id,
                TradingPairSymbol = p.TradingPair?.Symbol, // Assuming TradingPair is loaded or accessible
                PositionType = p.PositionType.ToString(),
                EntryPrice = p.EntryPrice,
                Quantity = p.Quantity,
                OpenTime = p.OpenTime,
                CloseTime = p.CloseTime,
                ExitPrice = p.ExitPrice,
                Status = p.Status.ToString(),
                StopLoss = p.StopLoss,
                TakeProfit = p.TakeProfit,
                // Add other relevant properties
            }).ToList();
        }

        /// <summary>
        /// Récupère une position par son identifiant
        /// </summary>
        /// <param name="id">Identifiant de la position</param>
        /// <returns>Le DTO de position ou null si non trouvé</returns>
        public async Task<PositionDTO?> GetPositionByIdAsync(long id)
        {
            var position = await _positionRepository.GetByIdAsync(id);
            if (position == null)
            {
                return null;
            }

            return new PositionDTO
            {
                Id = position.Id,
                TradingPairSymbol = position.TradingPair?.Symbol,
                PositionType = position.PositionType.ToString(),
                EntryPrice = position.EntryPrice,
                Quantity = position.Quantity,
                OpenTime = position.OpenTime,
                CloseTime = position.CloseTime,
                ExitPrice = position.ExitPrice,
                Status = position.Status.ToString(),
                StopLoss = position.StopLoss,
                TakeProfit = position.TakeProfit,
                // Add other relevant properties
            };
        }

        /// <summary>
        /// Ferme une position
        /// </summary>
        /// <param name="id">Identifiant de la position à fermer</param>
        /// <returns>Résultat du service avec le DTO de position mis à jour</returns>
        public async Task<ServiceResult<PositionDTO>> ClosePositionAsync(long id, decimal exitPrice)
        {
            // This method needs the current price to close the position.
            // A real implementation would fetch the current price from the exchange service.
            // For now, we'll use a placeholder or assume the repository handles it.
            // The IPositionRepository has a ClosePositionAsync method that takes exitPrice.
            // We need to decide how the service gets this exitPrice.
            // Option 1: Pass exitPrice to the service method.
            // Option 2: Service fetches exitPrice from an exchange service.
            // Let's assume Option 2 for better separation of concerns (Service handles business logic, Repository handles data).
            // This requires an IExchangeService dependency.

            // For now, let's use a dummy exit price or assume the repository handles fetching it internally (less ideal).
            // Let's modify the IPositionService and this implementation to accept exitPrice for simplicity in this step.
            // This means the IPositionService interface needs to be updated.

            // Re-evaluating the plan: I should update the IPositionService interface first if needed.
            // The current IPositionService.ClosePositionAsync(long id) does not take exitPrice.
            // The IPositionRepository.ClosePositionAsync(long id, decimal exitPrice) does.
            // The service should orchestrate the process, which includes getting the exit price.
            // Let's add an IExchangeService dependency to this service implementation.
            // This requires adding IExchangeService to the constructor and using it to get the current price.

            // Let's assume for now that the repository's ClosePositionAsync handles the exit price logic internally or it will be updated later.
            // This is a temporary simplification to proceed with the service implementation structure.

            var position = await _positionRepository.ClosePositionAsync(id, exitPrice);
            if (position == null)
            {
                return ServiceResult<PositionDTO>.Failure("Position not found.");
            }

            var positionDto = new PositionDTO
            {
                Id = position.Id,
                TradingPairSymbol = position.TradingPair?.Symbol,
                PositionType = position.PositionType.ToString(),
                EntryPrice = position.EntryPrice,
                Quantity = position.Quantity,
                OpenTime = position.OpenTime,
                CloseTime = position.CloseTime,
                ExitPrice = position.ExitPrice,
                Status = position.Status.ToString(),
                StopLoss = position.StopLoss,
                TakeProfit = position.TakeProfit,
                // Add other relevant properties
            };

            return ServiceResult<PositionDTO>.Success(positionDto);
        }

        /// <summary>
        /// Met à jour le Stop Loss et le Take Profit d'une position
        /// </summary>
        /// <param name="id">Identifiant de la position</param>
        /// <param name="stopLoss">Nouvelle valeur du Stop Loss</param>
        /// <param name="takeProfit">Nouvelle valeur du Take Profit</param>
        /// <returns>Résultat du service avec le DTO de position mis à jour</returns>
        public async Task<ServiceResult<PositionDTO>> UpdateStopLossTakeProfitAsync(long id, decimal? stopLoss, decimal? takeProfit)
        {
            var position = await _positionRepository.GetByIdAsync(id);
            if (position == null)
            {
                return ServiceResult<PositionDTO>.Failure("Position not found.");
            }

            position.StopLoss = stopLoss;
            position.TakeProfit = takeProfit;

            await _positionRepository.UpdateAsync(position);
            await _positionRepository.SaveChangesAsync();

            var positionDto = new PositionDTO
            {
                Id = position.Id,
                TradingPairSymbol = position.TradingPair?.Symbol,
                PositionType = position.PositionType.ToString(),
                EntryPrice = position.EntryPrice,
                Quantity = position.Quantity,
                OpenTime = position.OpenTime,
                CloseTime = position.CloseTime,
                ExitPrice = position.ExitPrice,
                Status = position.Status.ToString(),
                StopLoss = position.StopLoss,
                TakeProfit = position.TakeProfit,
                // Add other relevant properties
            };

            return ServiceResult<PositionDTO>.Success(positionDto);
        }
    }
}