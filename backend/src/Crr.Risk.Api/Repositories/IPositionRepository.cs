using Crr.Risk.Api.Models.Entities;
using Crr.Risk.Api.Querying;

namespace Crr.Risk.Api.Repositories;

public interface IPositionRepository
{
	Task<(IReadOnlyList<RiskPosition> Rows, long TotalCount)> QueryPositionsAsync(FilterCriteria criteria, CancellationToken cancellationToken);
	Task<RiskPosition?> GetByTradeIdAsync(string tradeId, CancellationToken cancellationToken);
	Task<(IReadOnlyList<AggregationBucket> Buckets, AggregationTotals Totals)> QueryAggregationsAsync(
		FilterCriteria criteria,
		string groupBy,
		CancellationToken cancellationToken);
}

public sealed class AggregationBucket
{
	public required string GroupKey { get; init; }
	public long RowCount { get; init; }
	public decimal PnlTotal { get; init; }
	public decimal FxExposureTotal { get; init; }
	public decimal? PositionTotal { get; init; }
}

public sealed class AggregationTotals
{
	public long RowCount { get; init; }
	public decimal PnlTotal { get; init; }
	public decimal FxExposureTotal { get; init; }
}
