using System.Globalization;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace Crr.Risk.Api.Querying;

public sealed class PositionQueryBuilder
{
	public (string WhereSql, List<NpgsqlParameter> Parameters) BuildWhereClause(FilterCriteria criteria)
	{
		var parameters = new List<NpgsqlParameter>();
		var clauses = new List<string>();

		foreach (var columnFilter in criteria.ColumnFilters)
		{
			if (!ColumnWhitelist.TryGet(columnFilter.ColumnId, out var definition))
			{
				throw new ArgumentException($"Unknown filter column '{columnFilter.ColumnId}'.");
			}

			if (!definition.FilterType.Equals(columnFilter.FilterType, StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException($"Column '{columnFilter.ColumnId}' expects filterType '{definition.FilterType}'.");
			}

			if (columnFilter.Conditions.Count == 0)
			{
				continue;
			}

			var conditionClauses = columnFilter.Conditions
				.Select(condition => BuildConditionSql(definition.DbColumn, definition.FilterType, condition, parameters))
				.ToArray();

			var logicalOperator = columnFilter.LogicalOperator.Equals("OR", StringComparison.OrdinalIgnoreCase)
				? " OR "
				: " AND ";

			clauses.Add($"({string.Join(logicalOperator, conditionClauses)})");
		}

		var normalizedSearch = (criteria.Search ?? string.Empty).Trim();
		if (!string.IsNullOrWhiteSpace(normalizedSearch))
		{
			var searchParameter = AddParameter(parameters, "search", normalizedSearch);
			clauses.Add($"(counterparty ILIKE '%' || {searchParameter} || '%' ESCAPE '\\' OR commodity ILIKE '%' || {searchParameter} || '%' ESCAPE '\\')");
		}

		var whereSql = clauses.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", clauses);
		return (whereSql, parameters);
	}

	public string BuildOrderByClause(FilterCriteria criteria)
	{
		var orderItems = new List<string>();

		foreach (var sort in criteria.Sorts)
		{
			var dbColumn = ColumnWhitelist.ResolveDbColumnOrThrow(sort.ColumnId);
			var direction = sort.Descending ? "DESC" : "ASC";
			orderItems.Add($"{dbColumn} {direction} NULLS LAST");
		}

		if (!orderItems.Any(item => item.StartsWith("trade_id", StringComparison.OrdinalIgnoreCase)))
		{
			orderItems.Add("trade_id ASC NULLS LAST");
		}

		return "ORDER BY " + string.Join(", ", orderItems);
	}

	public string BuildPagingClause(FilterCriteria criteria, List<NpgsqlParameter> parameters)
	{
		if (!criteria.UsePaging)
		{
			return string.Empty;
		}

		var pageSize = criteria.EndRow - criteria.StartRow;
		var limitParameter = AddParameter(parameters, "limit", pageSize, NpgsqlDbType.Integer);
		var offsetParameter = AddParameter(parameters, "offset", criteria.StartRow, NpgsqlDbType.Integer);
		return $"LIMIT {limitParameter} OFFSET {offsetParameter}";
	}

	private static string BuildConditionSql(string dbColumn, string filterType, FilterConditionCriterion condition, List<NpgsqlParameter> parameters)
	{
		var normalizedOperator = condition.Operator.Trim();

		return filterType.ToLowerInvariant() switch
		{
			"text" => BuildTextCondition(dbColumn, normalizedOperator, condition, parameters),
			"number" => BuildNumberCondition(dbColumn, normalizedOperator, condition, parameters),
			"date" => BuildDateCondition(dbColumn, normalizedOperator, condition, parameters),
			_ => throw new ArgumentException($"Unknown filter type '{filterType}'."),
		};
	}

	private static string BuildTextCondition(string dbColumn, string filterOperator, FilterConditionCriterion condition, List<NpgsqlParameter> parameters)
	{
		var value = EscapeLikeValue(condition.Filter ?? string.Empty);
		var parameter = AddParameter(parameters, "txt", value);

		return filterOperator switch
		{
			"contains" => $"{dbColumn} ILIKE '%' || {parameter} || '%' ESCAPE '\\'",
			"notContains" => $"NOT ({dbColumn} ILIKE '%' || {parameter} || '%' ESCAPE '\\')",
			"equals" => $"{dbColumn} ILIKE {parameter} ESCAPE '\\'",
			"notEqual" => $"{dbColumn} NOT ILIKE {parameter} ESCAPE '\\'",
			"startsWith" => $"{dbColumn} ILIKE {parameter} || '%' ESCAPE '\\'",
			"endsWith" => $"{dbColumn} ILIKE '%' || {parameter} ESCAPE '\\'",
			"blank" => $"({dbColumn} IS NULL OR {dbColumn} = '')",
			"notBlank" => $"({dbColumn} IS NOT NULL AND {dbColumn} <> '')",
			_ => throw new ArgumentException($"Unknown text operator '{filterOperator}'."),
		};
	}

	private static string BuildNumberCondition(string dbColumn, string filterOperator, FilterConditionCriterion condition, List<NpgsqlParameter> parameters)
	{
		return filterOperator switch
		{
			"equals" => $"{dbColumn} = {AddDecimalParameter(parameters, condition.Filter, "num")}",
			"notEqual" => $"{dbColumn} <> {AddDecimalParameter(parameters, condition.Filter, "num")}",
			"lessThan" => $"{dbColumn} < {AddDecimalParameter(parameters, condition.Filter, "num")}",
			"lessThanOrEqual" => $"{dbColumn} <= {AddDecimalParameter(parameters, condition.Filter, "num")}",
			"greaterThan" => $"{dbColumn} > {AddDecimalParameter(parameters, condition.Filter, "num")}",
			"greaterThanOrEqual" => $"{dbColumn} >= {AddDecimalParameter(parameters, condition.Filter, "num")}",
			"inRange" => BuildNumberRangeCondition(dbColumn, condition, parameters),
			"blank" => $"{dbColumn} IS NULL",
			"notBlank" => $"{dbColumn} IS NOT NULL",
			_ => throw new ArgumentException($"Unknown number operator '{filterOperator}'."),
		};
	}

	private static string BuildDateCondition(string dbColumn, string filterOperator, FilterConditionCriterion condition, List<NpgsqlParameter> parameters)
	{
		return filterOperator switch
		{
			"equals" => $"{dbColumn} = {AddDateParameter(parameters, condition.DateFrom ?? condition.Filter, "dt")}",
			"notEqual" => $"{dbColumn} <> {AddDateParameter(parameters, condition.DateFrom ?? condition.Filter, "dt")}",
			"lessThan" => $"{dbColumn} < {AddDateParameter(parameters, condition.DateFrom ?? condition.Filter, "dt")}",
			"greaterThan" => $"{dbColumn} > {AddDateParameter(parameters, condition.DateFrom ?? condition.Filter, "dt")}",
			"inRange" => BuildDateRangeCondition(dbColumn, condition, parameters),
			"blank" => $"{dbColumn} IS NULL",
			"notBlank" => $"{dbColumn} IS NOT NULL",
			_ => throw new ArgumentException($"Unknown date operator '{filterOperator}'."),
		};
	}

	private static string BuildNumberRangeCondition(string dbColumn, FilterConditionCriterion condition, List<NpgsqlParameter> parameters)
	{
		var parts = new List<string>();

		if (!string.IsNullOrWhiteSpace(condition.Filter))
		{
			parts.Add($"{dbColumn} >= {AddDecimalParameter(parameters, condition.Filter, "numFrom")}");
		}

		if (!string.IsNullOrWhiteSpace(condition.FilterTo))
		{
			parts.Add($"{dbColumn} <= {AddDecimalParameter(parameters, condition.FilterTo, "numTo")}");
		}

		if (parts.Count == 0)
		{
			throw new ArgumentException("inRange number filter requires filter and/or filterTo.");
		}

		return "(" + string.Join(" AND ", parts) + ")";
	}

	private static string BuildDateRangeCondition(string dbColumn, FilterConditionCriterion condition, List<NpgsqlParameter> parameters)
	{
		var from = condition.DateFrom ?? condition.Filter;
		var to = condition.DateTo ?? condition.FilterTo;
		var parts = new List<string>();

		if (!string.IsNullOrWhiteSpace(from))
		{
			parts.Add($"{dbColumn} >= {AddDateParameter(parameters, from, "dtFrom")}");
		}

		if (!string.IsNullOrWhiteSpace(to))
		{
			parts.Add($"{dbColumn} <= {AddDateParameter(parameters, to, "dtTo")}");
		}

		if (parts.Count == 0)
		{
			throw new ArgumentException("inRange date filter requires dateFrom/dateTo or filter/filterTo.");
		}

		return "(" + string.Join(" AND ", parts) + ")";
	}

	private static string AddDecimalParameter(List<NpgsqlParameter> parameters, string? rawValue, string namePrefix)
	{
		if (!decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
		{
			throw new ArgumentException($"Expected a valid decimal value for '{namePrefix}'.");
		}

		return AddParameter(parameters, namePrefix, value, NpgsqlDbType.Numeric);
	}

	private static string AddDateParameter(List<NpgsqlParameter> parameters, string? rawValue, string namePrefix)
	{
		if (!DateOnly.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
		{
			throw new ArgumentException($"Expected a valid ISO date value for '{namePrefix}'.");
		}

		return AddParameter(parameters, namePrefix, value, NpgsqlDbType.Date);
	}

	private static string AddParameter(List<NpgsqlParameter> parameters, string namePrefix, object value, NpgsqlDbType? dbType = null)
	{
		var parameterName = $"@p{parameters.Count}_{namePrefix}";
		var parameter = dbType.HasValue
			? new NpgsqlParameter(parameterName, dbType.Value) { Value = value }
			: new NpgsqlParameter(parameterName, value);

		parameters.Add(parameter);
		return parameterName;
	}

	private static string EscapeLikeValue(string input)
	{
		var builder = new StringBuilder(input.Length);
		foreach (var ch in input)
		{
			if (ch is '%' or '_' or '\\')
			{
				builder.Append('\\');
			}

			builder.Append(ch);
		}

		return builder.ToString();
	}
}
