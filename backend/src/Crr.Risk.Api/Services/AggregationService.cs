using Crr.Risk.Api.Models.Dtos;
using Crr.Risk.Api.Repositories;
using Crr.Risk.Api.Validation;

namespace Crr.Risk.Api.Services;

public sealed class AggregationService : IAggregationService
{
	private const string MixedUnitsNote = "Position is not aggregated outside commodity grouping because units differ across commodities.";

	private readonly RowsRequestValidator _validator;
	private readonly IPositionRepository _repository;

	public AggregationService(RowsRequestValidator validator, IPositionRepository repository)
	{
		_validator = validator;
		_repository = repository;
	}

	public async Task<AggregationResponseDto> GetAggregationsAsync(AggregationRequestDto request, CancellationToken cancellationToken)
	{
		var groupBy = request.GroupBy?.Trim();
		if (string.IsNullOrWhiteSpace(groupBy))
		{
			throw new ArgumentException("groupBy is required.");
		}

		var criteria = _validator.CreateForAggregation(request);
		var (buckets, totals) = await _repository.QueryAggregationsAsync(criteria, groupBy, cancellationToken);

		var isCommodityGrouping = groupBy.Equals("commodity", StringComparison.OrdinalIgnoreCase);

		return new AggregationResponseDto
		{
			Groups = buckets.Select(bucket => new AggregationGroupDto
			{
				GroupKey = bucket.GroupKey,
				RowCount = bucket.RowCount,
				PnlTotal = bucket.PnlTotal,
				FxExposureTotal = bucket.FxExposureTotal,
				PositionTotal = isCommodityGrouping ? bucket.PositionTotal : null,
				PositionNote = isCommodityGrouping ? "units per commodity" : MixedUnitsNote,
			}).ToArray(),
			GrandTotals = new AggregationTotalsDto
			{
				RowCount = totals.RowCount,
				PnlTotal = totals.PnlTotal,
				FxExposureTotal = totals.FxExposureTotal,
			},
			PositionNote = isCommodityGrouping ? "units per commodity" : MixedUnitsNote,
		};
	}
}
