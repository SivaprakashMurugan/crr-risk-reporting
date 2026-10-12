using Microsoft.AspNetCore.Mvc;
using Crr.Risk.Api.Models.Dtos;
using Crr.Risk.Api.Services;

namespace Crr.Risk.Api.Controllers;

[ApiController]
[Route("api/risk/positions")]
public sealed class PositionsController : ControllerBase
{
    private readonly IPositionService _positionService;
    private readonly ILogger<PositionsController> _logger;

    public PositionsController(IPositionService positionService, ILogger<PositionsController> logger)
    {
        _positionService = positionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ListPositions([FromBody] RowsRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _positionService.GetPositionsAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(499, new { error = "Request was cancelled." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while listing positions.");
            return StatusCode(500, new { error = "Failed to load position rows." });
        }
    }

    [HttpGet("{tradeId}")]
    public async Task<IActionResult> GetPositionByTradeId(string tradeId, CancellationToken cancellationToken)
    {
        try
        {
            var row = await _positionService.GetPositionByTradeIdAsync(tradeId, cancellationToken);
            return row is null ? NotFound() : Ok(row);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(499, new { error = "Request was cancelled." });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while loading position detail for tradeId {TradeId}.", tradeId);
            return StatusCode(500, new { error = "Failed to load trade detail." });
        }
    }
}
