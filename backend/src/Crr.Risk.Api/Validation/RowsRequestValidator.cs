using Crr.Risk.Api.Models.Dtos;
using Crr.Risk.Api.Querying;
using System.Text.Json;

namespace Crr.Risk.Api.Validation;

public sealed class RowsRequestValidator
{
	private const int MaxPageSize = 500;
	private const int MaxSearchLength = 200;
	private readonly AgGridFilterParser _filterParser;

	public RowsRequestValidator(AgGridFilterParser filterParser)
	{
		_filterParser = filterParser;
	}

	public FilterCriteria CreateForRows(RowsRequestDto request)
	{
		if (request.StartRow < 0)
		{
			throw new ArgumentException("startRow must be >= 0.");
		}

		if (request.EndRow <= request.StartRow)
		{
			throw new ArgumentException("endRow must be greater than startRow.");
		}

		if (request.EndRow - request.StartRow > MaxPageSize)
		{
			throw new ArgumentException($"Requested page size exceeds maximum of {MaxPageSize}.");
		}

		return CreateCriteria(
			request.FilterModel,
			request.SortModel,
			request.Search,
			request.StartRow,
			request.EndRow,
			usePaging: true);
	}

	public FilterCriteria CreateForAggregation(AggregationRequestDto request)
	{
		return CreateCriteria(request.FilterModel, request.SortModel, request.Search, 0, 0, usePaging: false);
	}

	private FilterCriteria CreateCriteria(
		JsonElement filterModel,
		IReadOnlyList<SortModelDto> sortModel,
		string? search,
		int startRow,
		int endRow,
		bool usePaging)
	{
		var sorts = sortModel
			.Select(item =>
			{
				if (!ColumnWhitelist.TryGet(item.ColId, out _))
				{
					throw new ArgumentException($"Unknown sort column '{item.ColId}'.");
				}

				var sort = item.Sort?.Trim().ToLowerInvariant();
				return sort switch
				{
					"asc" => new SortCriterion(item.ColId, Descending: false),
					"desc" => new SortCriterion(item.ColId, Descending: true),
					_ => throw new ArgumentException($"Unknown sort direction '{item.Sort}' for column '{item.ColId}'."),
				};
			})
			.ToList();

		var normalizedSearch = search?.Trim();
		if (!string.IsNullOrWhiteSpace(normalizedSearch) && normalizedSearch.Length > MaxSearchLength)
		{
			throw new ArgumentException($"search must be <= {MaxSearchLength} characters.");
		}

		return new FilterCriteria
		{
			StartRow = startRow,
			EndRow = endRow,
			UsePaging = usePaging,
			Search = normalizedSearch,
			Sorts = sorts,
			ColumnFilters = _filterParser.Parse(filterModel),
		};
	}
}
