using Crr.Risk.Api.Models.Dtos;

namespace Crr.Risk.Api.Services;

public interface IPositionService
{
	Task<RowsResponseDto> GetPositionsAsync(RowsRequestDto request, CancellationToken cancellationToken);
	Task<RiskPositionDto?> GetPositionByTradeIdAsync(string tradeId, CancellationToken cancellationToken);
}
