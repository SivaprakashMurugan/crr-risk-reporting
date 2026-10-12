using Crr.Risk.Api.Models.Dtos;
using Crr.Risk.Api.Models.Entities;
using Crr.Risk.Api.Repositories;
using Crr.Risk.Api.Validation;

namespace Crr.Risk.Api.Services;

public sealed class PositionService : IPositionService
{
	private readonly RowsRequestValidator _validator;
	private readonly IPositionRepository _repository;

	public PositionService(RowsRequestValidator validator, IPositionRepository repository)
	{
		_validator = validator;
		_repository = repository;
	}

	public async Task<RowsResponseDto> GetPositionsAsync(RowsRequestDto request, CancellationToken cancellationToken)
	{
		var criteria = _validator.CreateForRows(request);
		var (rows, totalCount) = await _repository.QueryPositionsAsync(criteria, cancellationToken);

		return new RowsResponseDto
		{
			Rows = rows.Select(Map).ToArray(),
			LastRow = totalCount,
		};
	}

	public async Task<RiskPositionDto?> GetPositionByTradeIdAsync(string tradeId, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(tradeId))
		{
			return null;
		}

		var row = await _repository.GetByTradeIdAsync(tradeId.Trim(), cancellationToken);
		return row is null ? null : Map(row);
	}

	private static RiskPositionDto Map(RiskPosition row)
	{
		return new RiskPositionDto
		{
			TradeId = row.TradeId,
			Book = row.Book,
			Commodity = row.Commodity,
			Counterparty = row.Counterparty,
			Region = row.Region,
			InstrumentType = row.InstrumentType,
			Position = row.Position,
			Currency = row.Currency,
			MarketPrice = row.MarketPrice,
			Pnl = row.Pnl,
			FxExposure = row.FxExposure,
			AsOfDate = row.AsOfDate,
		};
	}
}
