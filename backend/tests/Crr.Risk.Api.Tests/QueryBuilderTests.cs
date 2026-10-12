using Crr.Risk.Api.Querying;

namespace Crr.Risk.Api.Tests;

public sealed class QueryBuilderTests
{
	private readonly PositionQueryBuilder _builder = new();

	[Fact]
	public void BuildOrderByClause_UsesTradeIdFallback_WhenSortMissing()
	{
		var criteria = new FilterCriteria();
		var sql = _builder.BuildOrderByClause(criteria);
		Assert.Contains("trade_id ASC", sql);
	}

	[Fact]
	public void BuildWhereClause_BuildsNumberAndDateRanges()
	{
		var criteria = new FilterCriteria
		{
			ColumnFilters =
			[
				new ColumnFilterCriterion
				{
					ColumnId = "pnl",
					FilterType = "number",
					Conditions = [ new FilterConditionCriterion { Operator = "inRange", Filter = "10", FilterTo = "20" } ],
				},
				new ColumnFilterCriterion
				{
					ColumnId = "asOfDate",
					FilterType = "date",
					Conditions = [ new FilterConditionCriterion { Operator = "inRange", DateFrom = "2026-06-01", DateTo = "2026-06-30" } ],
				},
			],
		};

		var (whereSql, parameters) = _builder.BuildWhereClause(criteria);
		Assert.Contains("pnl >=", whereSql);
		Assert.Contains("pnl <=", whereSql);
		Assert.Contains("as_of_date >=", whereSql);
		Assert.Contains("as_of_date <=", whereSql);
		Assert.NotEmpty(parameters);
	}

	[Fact]
	public void BuildWhereClause_EscapesLikeWildcards()
	{
		var criteria = new FilterCriteria
		{
			ColumnFilters =
			[
				new ColumnFilterCriterion
				{
					ColumnId = "counterparty",
					FilterType = "text",
					Conditions = [ new FilterConditionCriterion { Operator = "contains", Filter = "A_B%" } ],
				},
			],
		};

		var (_, parameters) = _builder.BuildWhereClause(criteria);
		var parameterValue = Assert.Single(parameters).Value?.ToString();
		Assert.Equal("A\\_B\\%", parameterValue);
	}

	[Fact]
	public void BuildWhereClause_UsesSingleCharacterEscapeLiteral()
	{
		var criteria = new FilterCriteria
		{
			ColumnFilters =
			[
				new ColumnFilterCriterion
				{
					ColumnId = "book",
					FilterType = "text",
					Conditions = [ new FilterConditionCriterion { Operator = "equals", Filter = "Ags" } ],
				},
			],
			Search = "metals",
		};

		var (whereSql, _) = _builder.BuildWhereClause(criteria);

		Assert.Contains("ESCAPE '\\'", whereSql);
		Assert.DoesNotContain("ESCAPE '\\\\'", whereSql.Replace("ESCAPE '\\'", string.Empty));
	}
}
