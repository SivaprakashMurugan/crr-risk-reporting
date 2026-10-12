using System.Text.Json;
using Crr.Risk.Api.Querying;

namespace Crr.Risk.Api.Tests;

public sealed class FilterParserTests
{
		private readonly AgGridFilterParser _parser = new();

		[Fact]
		public void Parse_SupportsLegacyCondition1Condition2_WithOperator()
		{
				var json = """
				{
					"book": {
						"filterType": "text",
						"operator": "OR",
						"condition1": { "type": "equals", "filter": "Ags" },
						"condition2": { "type": "equals", "filter": "Freight" }
					}
				}
				""";

				using var doc = JsonDocument.Parse(json);
				var filters = _parser.Parse(doc.RootElement);

				var bookFilter = Assert.Single(filters);
				Assert.Equal("OR", bookFilter.LogicalOperator);
				Assert.Equal(2, bookFilter.Conditions.Count);
				Assert.Equal("equals", bookFilter.Conditions[0].Operator);
		}

		[Fact]
		public void Parse_SupportsConditionsArray()
		{
				var json = """
				{
					"pnl": {
						"filterType": "number",
						"operator": "AND",
						"conditions": [
							{ "type": "greaterThan", "filter": 100 },
							{ "type": "lessThan", "filter": 200 }
						]
					}
				}
				""";

				using var doc = JsonDocument.Parse(json);
				var filters = _parser.Parse(doc.RootElement);

				var pnlFilter = Assert.Single(filters);
				Assert.Equal(2, pnlFilter.Conditions.Count);
				Assert.Equal("greaterThan", pnlFilter.Conditions[0].Operator);
				Assert.Equal("100", pnlFilter.Conditions[0].Filter);
		}

		[Fact]
		public void Parse_UnknownColumn_Throws()
		{
				var json = """
				{
					"unknownField": {
						"filterType": "text",
						"type": "contains",
						"filter": "x"
					}
				}
				""";

				using var doc = JsonDocument.Parse(json);
				Assert.Throws<ArgumentException>(() => _parser.Parse(doc.RootElement));
		}
}
