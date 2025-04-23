using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Added missing using directive
using BinanceTradingBot.Domain.Entities;
using BinanceTradingBot.WebDashboard.Services;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic; // Added missing using directive
using System; // Added missing using directive for Exception

namespace BinanceTradingBot.WebDashboard.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "UserAccess")]
    public class TradingPairsController : ControllerBase
    {
        private readonly ITradingPairService _tradingPairService;
        private readonly ILogger<TradingPairsController> _logger;

        public TradingPairsController(ITradingPairService tradingPairService, ILogger<TradingPairsController> logger)
        {
            _tradingPairService = tradingPairService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TradingPairDTO>>> GetAllTradingPairs()
        {
            try
            {
                var pairs = await _tradingPairService.GetAllTradingPairsAsync();
                return Ok(pairs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paires de trading");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des paires de trading");
            }
        }

        [HttpGet("{symbol}")]
        public async Task<ActionResult<TradingPairDTO>> GetTradingPair(string symbol)
        {
            try
            {
                var pair = await _tradingPairService.GetTradingPairBySymbolAsync(symbol);
                if (pair == null)
                {
                    return NotFound($"Paire de trading '{symbol}' non trouvée");
                }
                return Ok(pair);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la paire de trading {Symbol}", symbol);
                return StatusCode(500, $"Une erreur est survenue lors de la récupération de la paire {symbol}");
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<TradingPairDTO>> CreateTradingPair(TradingPairDTO pairDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _tradingPairService.CreateTradingPairAsync(pairDTO);
                return CreatedAtAction(nameof(GetTradingPair), new { symbol = result.Symbol }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la paire de trading {Symbol}", pairDTO.Symbol);
                return StatusCode(500, "Une erreur est survenue lors de la création de la paire de trading");
            }
        }

        [HttpPut("{symbol}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateTradingPair(string symbol, TradingPairDTO pairDTO)
        {
            try
            {
                if (symbol != pairDTO.Symbol)
                {
                    return BadRequest("Le symbole de la paire ne correspond pas à l'URL");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updated = await _tradingPairService.UpdateTradingPairAsync(pairDTO);
                if (!updated)
                {
                    return NotFound($"Paire de trading '{symbol}' non trouvée");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la paire de trading {Symbol}", symbol);
                return StatusCode(500, $"Une erreur est survenue lors de la mise à jour de la paire {symbol}");
            }
        }

        [HttpPatch("{symbol}/toggle-active")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> TogglePairActive(string symbol)
        {
            try
            {
                var updated = await _tradingPairService.ToggleTradingPairActiveAsync(symbol);
                if (!updated)
                {
                    return NotFound($"Paire de trading '{symbol}' non trouvée");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du basculement de l'état actif de la paire {Symbol}", symbol);
                return StatusCode(500, $"Une erreur est survenue lors du basculement de l'état actif de la paire {symbol}");
            }
        }
    }
}