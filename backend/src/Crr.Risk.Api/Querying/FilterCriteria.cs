namespace Crr.Risk.Api.Querying;

public sealed class FilterCriteria
{
	public int StartRow { get; init; }
	public int EndRow { get; init; }
	public bool UsePaging { get; init; }
	public string? Search { get; init; }
	public IReadOnlyList<SortCriterion> Sorts { get; init; } = [];
	public IReadOnlyList<ColumnFilterCriterion> ColumnFilters { get; init; } = [];
}

public sealed record SortCriterion(string ColumnId, bool Descending);

public sealed class ColumnFilterCriterion
{
	public required string ColumnId { get; init; }
	public required string FilterType { get; init; }
	public string LogicalOperator { get; init; } = "AND";
	public IReadOnlyList<FilterConditionCriterion> Conditions { get; init; } = [];
}

public sealed class FilterConditionCriterion
{
	public required string Operator { get; init; }
	public string? Filter { get; init; }
	public string? FilterTo { get; init; }
	public string? DateFrom { get; init; }
	public string? DateTo { get; init; }
}
