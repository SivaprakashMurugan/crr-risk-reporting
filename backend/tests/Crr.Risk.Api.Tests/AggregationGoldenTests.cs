using Crr.Risk.Api.Models.Dtos;
using Crr.Risk.Api.Models.Entities;
using Crr.Risk.Api.Querying;
using Crr.Risk.Api.Repositories;
using Crr.Risk.Api.Services;
using Crr.Risk.Api.Validation;
using Microsoft.Extensions.Options;

namespace Crr.Risk.Api.Tests;

public sealed class AggregationGoldenTests
{
	[Fact]
	public async Task AggregationService_NonCommodityGrouping_HidesPositionTotals()
	{
		var service = CreateService();
		var request = new AggregationRequestDto
		{
			GroupBy = "book",
		};

		var response = await service.GetAggregationsAsync(request, CancellationToken.None);

		Assert.All(response.Groups, group => Assert.Null(group.PositionTotal));
		Assert.Contains("not aggregated", response.PositionNote, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task AggregationService_CommodityGrouping_KeepsPositionTotals()
	{
		var service = CreateService();
		var request = new AggregationRequestDto
		{
			GroupBy = "commodity",
		};

		var response = await service.GetAggregationsAsync(request, CancellationToken.None);

		Assert.All(response.Groups, group => Assert.NotNull(group.PositionTotal));
		Assert.Contains("units per commodity", response.PositionNote, StringComparison.OrdinalIgnoreCase);
	}

	private static AggregationService CreateService()
	{
		var parser = new AgGridFilterParser();
		var validator = new RowsRequestValidator(parser);
		var repository = new FakePositionRepository();
		return new AggregationService(validator, repository);
	}

	private sealed class FakePositionRepository : IPositionRepository
	{
		public Task<RiskPosition?> GetByTradeIdAsync(string tradeId, CancellationToken cancellationToken)
			=> Task.FromResult<RiskPosition?>(null);

		public Task<(IReadOnlyList<AggregationBucket> Buckets, AggregationTotals Totals)> QueryAggregationsAsync(
			FilterCriteria criteria,
			string groupBy,
			CancellationToken cancellationToken)
		{
			var includePosition = groupBy.Equals("commodity", StringComparison.OrdinalIgnoreCase);
			var buckets = new List<AggregationBucket>
			{
				new()
				{
					GroupKey = "Ags",
					RowCount = 2,
					PnlTotal = 100,
					FxExposureTotal = 200,
					PositionTotal = includePosition ? 10 : null,
				},
			};

			var totals = new AggregationTotals
			{
				RowCount = 2,
				PnlTotal = 100,
				FxExposureTotal = 200,
			};

			return Task.FromResult<(IReadOnlyList<AggregationBucket>, AggregationTotals)>((buckets, totals));
		}

		public Task<(IReadOnlyList<RiskPosition> Rows, long TotalCount)> QueryPositionsAsync(FilterCriteria criteria, CancellationToken cancellationToken)
		{
			return Task.FromResult<(IReadOnlyList<RiskPosition>, long)>(([], 0L));
		}
	}
}
