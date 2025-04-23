using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BinanceTradingBot.WebDashboard.Services;
using BinanceTradingBot.WebDashboard.Models.DTOs;
using System.Threading.Tasks;

namespace BinanceTradingBot.WebDashboard.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "UserAccess")]
    public class PositionsController : ControllerBase
    {
        private readonly IPositionService _positionService;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(IPositionService positionService, ILogger<PositionsController> logger)
        {
            _positionService = positionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PositionDTO>>> GetAllPositions([FromQuery] bool activeOnly = false)
        {
            try
            {
                var positions = await _positionService.GetPositionsAsync(activeOnly);
                return Ok(positions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des positions");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des positions");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PositionDTO>> GetPosition(long id)
        {
            try
            {
                var position = await _positionService.GetPositionByIdAsync(id);
                if (position == null)
                {
                    return NotFound($"Position {id} non trouvée");
                }
                return Ok(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la position {Id}", id);
                return StatusCode(500, $"Une erreur est survenue lors de la récupération de la position {id}");
            }
        }

        [HttpPost("{id}/close")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ClosePosition(long id)
        {
            try
            {
                var result = await _positionService.ClosePositionAsync(id);
                if (!result.Success)
                {
                    return result.NotFound ? NotFound(result.Message) : BadRequest(result.Message);
                }
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la fermeture de la position {Id}", id);
                return StatusCode(500, $"Une erreur est survenue lors de la fermeture de la position {id}");
            }
        }

        [HttpPatch("{id}/update-sl-tp")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateStopLossAndTakeProfit(long id, [FromBody] StopLossTakeProfitUpdateDTO update)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _positionService.UpdateStopLossTakeProfitAsync(id, update.StopLoss, update.TakeProfit);
                if (!result.Success)
                {
                    return result.NotFound ? NotFound(result.Message) : BadRequest(result.Message);
                }
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour SL/TP de la position {Id}", id);
                return StatusCode(500, $"Une erreur est survenue lors de la mise à jour SL/TP de la position {id}");
            }
        }
    }
}