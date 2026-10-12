namespace Crr.Risk.Api.Querying;

public static class ColumnWhitelist
{
	private static readonly IReadOnlyDictionary<string, ColumnDefinition> Definitions =
		new Dictionary<string, ColumnDefinition>(StringComparer.OrdinalIgnoreCase)
		{
			["tradeId"] = new("trade_id", "text"),
			["book"] = new("book", "text"),
			["commodity"] = new("commodity", "text"),
			["counterparty"] = new("counterparty", "text"),
			["region"] = new("region", "text"),
			["instrumentType"] = new("instrument_type", "text"),
			["position"] = new("position", "number"),
			["currency"] = new("currency", "text"),
			["marketPrice"] = new("market_price", "number"),
			["pnl"] = new("pnl", "number"),
			["fxExposure"] = new("fx_exposure", "number"),
			["asOfDate"] = new("as_of_date", "date"),
		};

	public static IReadOnlyCollection<string> AllowedColumns => Definitions.Keys.ToArray();

	public static bool TryGet(string apiColumn, out ColumnDefinition definition) => Definitions.TryGetValue(apiColumn, out definition!);

	public static bool IsGroupByColumnAllowed(string apiColumn) =>
		apiColumn.Equals("book", StringComparison.OrdinalIgnoreCase)
		|| apiColumn.Equals("commodity", StringComparison.OrdinalIgnoreCase)
		|| apiColumn.Equals("region", StringComparison.OrdinalIgnoreCase)
		|| apiColumn.Equals("instrumentType", StringComparison.OrdinalIgnoreCase);

	public static string ResolveDbColumnOrThrow(string apiColumn)
	{
		if (!TryGet(apiColumn, out var definition))
		{
			throw new ArgumentException($"Unknown column '{apiColumn}'.", nameof(apiColumn));
		}

		return definition.DbColumn;
	}

	public sealed record ColumnDefinition(string DbColumn, string FilterType);
}
