using System.Text.Json;

namespace Crr.Risk.Api.Querying;

public sealed class AgGridFilterParser
{
	public IReadOnlyList<ColumnFilterCriterion> Parse(JsonElement filterModel)
	{
		if (filterModel.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
		{
			return [];
		}

		if (filterModel.ValueKind != JsonValueKind.Object)
		{
			throw new ArgumentException("filterModel must be a JSON object.");
		}

		var filters = new List<ColumnFilterCriterion>();

		foreach (var property in filterModel.EnumerateObject())
		{
			if (!ColumnWhitelist.TryGet(property.Name, out var columnDefinition))
			{
				throw new ArgumentException($"Unknown filter column '{property.Name}'.");
			}

			if (property.Value.ValueKind != JsonValueKind.Object)
			{
				throw new ArgumentException($"Filter definition for '{property.Name}' must be an object.");
			}

			var node = property.Value;
			var filterType = GetString(node, "filterType") ?? columnDefinition.FilterType;
			var logicalOperator = (GetString(node, "operator") ?? "AND").ToUpperInvariant();
			if (logicalOperator is not ("AND" or "OR"))
			{
				throw new ArgumentException($"Unknown logical operator '{logicalOperator}' for column '{property.Name}'.");
			}

			var conditions = new List<FilterConditionCriterion>();

			if (node.TryGetProperty("conditions", out var conditionsNode) && conditionsNode.ValueKind == JsonValueKind.Array)
			{
				foreach (var condition in conditionsNode.EnumerateArray())
				{
					conditions.Add(ParseCondition(condition));
				}
			}
			else if (node.TryGetProperty("condition1", out var condition1))
			{
				conditions.Add(ParseCondition(condition1));

				if (node.TryGetProperty("condition2", out var condition2))
				{
					conditions.Add(ParseCondition(condition2));
				}
			}
			else
			{
				conditions.Add(ParseCondition(node));
			}

			filters.Add(new ColumnFilterCriterion
			{
				ColumnId = property.Name,
				FilterType = filterType,
				LogicalOperator = logicalOperator,
				Conditions = conditions,
			});
		}

		return filters;
	}

	private static FilterConditionCriterion ParseCondition(JsonElement node)
	{
		var type = GetString(node, "type");
		if (string.IsNullOrWhiteSpace(type))
		{
			throw new ArgumentException("Each filter condition must include a type.");
		}

		return new FilterConditionCriterion
		{
			Operator = type,
			Filter = GetScalarAsString(node, "filter"),
			FilterTo = GetScalarAsString(node, "filterTo"),
			DateFrom = GetScalarAsString(node, "dateFrom"),
			DateTo = GetScalarAsString(node, "dateTo"),
		};
	}

	private static string? GetString(JsonElement element, string propertyName)
	{
		if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
		{
			return null;
		}

		return value.ValueKind == JsonValueKind.String
			? value.GetString()
			: value.GetRawText();
	}

	private static string? GetScalarAsString(JsonElement element, string propertyName)
	{
		if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
		{
			return null;
		}

		return value.ValueKind switch
		{
			JsonValueKind.String => value.GetString(),
			JsonValueKind.Number => value.GetRawText(),
			JsonValueKind.True => bool.TrueString,
			JsonValueKind.False => bool.FalseString,
			_ => null,
		};
	}
}
