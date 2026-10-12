using Crr.Risk.Api.Models.Dtos;

namespace Crr.Risk.Api.Services;

public interface IAggregationService
{
	Task<AggregationResponseDto> GetAggregationsAsync(AggregationRequestDto request, CancellationToken cancellationToken);
}
