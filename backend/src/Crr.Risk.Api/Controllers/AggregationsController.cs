using Microsoft.AspNetCore.Mvc;
using Crr.Risk.Api.Models.Dtos;
using Crr.Risk.Api.Services;

namespace Crr.Risk.Api.Controllers;

[ApiController]
[Route("api/risk/aggregations")]
public sealed class AggregationsController : ControllerBase
{
    private readonly IAggregationService _aggregationService;
    private readonly ILogger<AggregationsController> _logger;

    public AggregationsController(IAggregationService aggregationService, ILogger<AggregationsController> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> GetAggregations([FromBody] AggregationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _aggregationService.GetAggregationsAsync(request, cancellationToken);
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
            _logger.LogError(exception, "Unexpected error while loading aggregations.");
            return StatusCode(500, new { error = "Failed to load aggregation data." });
        }
    }
}
