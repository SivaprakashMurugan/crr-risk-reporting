using Crr.Risk.Api.Models.Entities;
using Crr.Risk.Api.Querying;
using Npgsql;
using NpgsqlTypes;

namespace Crr.Risk.Api.Repositories;

public sealed class PositionRepository : IPositionRepository
{
	private readonly IConfiguration _configuration;
	private readonly PositionQueryBuilder _queryBuilder;

	public PositionRepository(IConfiguration configuration, PositionQueryBuilder queryBuilder)
	{
		_configuration = configuration;
		_queryBuilder = queryBuilder;
	}

	public async Task<(IReadOnlyList<RiskPosition> Rows, long TotalCount)> QueryPositionsAsync(FilterCriteria criteria, CancellationToken cancellationToken)
	{
		await using var connection = await OpenConnectionAsync(cancellationToken);
		var (whereSql, parameters) = _queryBuilder.BuildWhereClause(criteria);
		var orderBySql = _queryBuilder.BuildOrderByClause(criteria);
		var pagingSql = _queryBuilder.BuildPagingClause(criteria, parameters);

		var countSql = $"SELECT COUNT(*) FROM risk_positions {whereSql}";
		await using var countCommand = new NpgsqlCommand(countSql, connection);
		foreach (var parameter in parameters.Where(p => !p.ParameterName.Contains("limit", StringComparison.OrdinalIgnoreCase) && !p.ParameterName.Contains("offset", StringComparison.OrdinalIgnoreCase)))
		{
			AddParameter(countCommand, parameter);
		}

		var totalCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken));

		var selectSql = $"""
			SELECT trade_id, book, commodity, counterparty, region, instrument_type,
				   position, currency, market_price, pnl, fx_exposure, as_of_date
			FROM risk_positions
			{whereSql}
			{orderBySql}
			{pagingSql}
			""";

		await using var command = new NpgsqlCommand(selectSql, connection);
		foreach (var parameter in parameters)
		{
			AddParameter(command, parameter);
		}

		var rows = new List<RiskPosition>();
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			rows.Add(MapRow(reader));
		}

		return (rows, totalCount);
	}

	public async Task<RiskPosition?> GetByTradeIdAsync(string tradeId, CancellationToken cancellationToken)
	{
		await using var connection = await OpenConnectionAsync(cancellationToken);
		const string sql = """
			SELECT trade_id, book, commodity, counterparty, region, instrument_type,
				   position, currency, market_price, pnl, fx_exposure, as_of_date
			FROM risk_positions
			WHERE trade_id = @tradeId
			""";

		await using var command = new NpgsqlCommand(sql, connection);
		command.Parameters.AddWithValue("@tradeId", tradeId);

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		return await reader.ReadAsync(cancellationToken) ? MapRow(reader) : null;
	}

	public async Task<(IReadOnlyList<AggregationBucket> Buckets, AggregationTotals Totals)> QueryAggregationsAsync(
		FilterCriteria criteria,
		string groupBy,
		CancellationToken cancellationToken)
	{
		if (!ColumnWhitelist.IsGroupByColumnAllowed(groupBy))
		{
			throw new ArgumentException($"Unknown groupBy column '{groupBy}'.");
		}

		var groupByColumn = ColumnWhitelist.ResolveDbColumnOrThrow(groupBy);

		await using var connection = await OpenConnectionAsync(cancellationToken);
		var (whereSql, parameters) = _queryBuilder.BuildWhereClause(criteria);

		var positionSql = groupBy.Equals("commodity", StringComparison.OrdinalIgnoreCase)
			? "SUM(position) AS position_total"
			: "NULL::numeric AS position_total";

		var groupedSql = $"""
			SELECT {groupByColumn} AS group_key,
				   COUNT(*)::bigint AS row_count,
				   COALESCE(SUM(pnl), 0)::numeric AS pnl_total,
				   COALESCE(SUM(fx_exposure), 0)::numeric AS fx_exposure_total,
				   {positionSql}
			FROM risk_positions
			{whereSql}
			GROUP BY {groupByColumn}
			ORDER BY {groupByColumn} ASC
			""";

		await using var groupCommand = new NpgsqlCommand(groupedSql, connection);
		foreach (var parameter in parameters)
		{
			AddParameter(groupCommand, parameter);
		}

		var buckets = new List<AggregationBucket>();
		await using (var reader = await groupCommand.ExecuteReaderAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				var groupKey = reader.IsDBNull(0) ? "(blank)" : reader.GetString(0);

				buckets.Add(new AggregationBucket
				{
					GroupKey = groupKey,
					RowCount = reader.GetInt64(1),
					PnlTotal = reader.GetDecimal(2),
					FxExposureTotal = reader.GetDecimal(3),
					PositionTotal = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
				});
			}
		}

		var totalsSql = $"""
			SELECT COUNT(*)::bigint AS row_count,
				   COALESCE(SUM(pnl), 0)::numeric AS pnl_total,
				   COALESCE(SUM(fx_exposure), 0)::numeric AS fx_exposure_total
			FROM risk_positions
			{whereSql}
			""";

		await using var totalsCommand = new NpgsqlCommand(totalsSql, connection);
		foreach (var parameter in parameters)
		{
			AddParameter(totalsCommand, parameter);
		}

		await using var totalsReader = await totalsCommand.ExecuteReaderAsync(cancellationToken);
		await totalsReader.ReadAsync(cancellationToken);
		var totals = new AggregationTotals
		{
			RowCount = totalsReader.GetInt64(0),
			PnlTotal = totalsReader.GetDecimal(1),
			FxExposureTotal = totalsReader.GetDecimal(2),
		};

		return (buckets, totals);
	}

	private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
	{
		var connectionString = _configuration.GetConnectionString("RiskDb")
			?? throw new InvalidOperationException("Connection string 'RiskDb' is missing.");

		var connection = new NpgsqlConnection(connectionString);
		await connection.OpenAsync(cancellationToken);
		return connection;
	}

	private static RiskPosition MapRow(NpgsqlDataReader reader)
	{
		return new RiskPosition
		{
			TradeId = reader.GetString(0),
			Book = reader.GetString(1),
			Commodity = reader.GetString(2),
			Counterparty = reader.GetString(3),
			Region = reader.GetString(4),
			InstrumentType = reader.GetString(5),
			Position = reader.GetDecimal(6),
			Currency = reader.GetString(7),
			MarketPrice = reader.GetDecimal(8),
			Pnl = reader.GetDecimal(9),
			FxExposure = reader.GetDecimal(10),
			AsOfDate = reader.GetFieldValue<DateOnly>(11),
		};
	}

	private static void AddParameter(NpgsqlCommand command, NpgsqlParameter source)
	{
		if (source.Value is null)
		{
			command.Parameters.AddWithValue(source.ParameterName, DBNull.Value);
			return;
		}

		try
		{
			var dbType = source.NpgsqlDbType;
			if (dbType != NpgsqlDbType.Unknown)
			{
				command.Parameters.Add(source.ParameterName, dbType).Value = source.Value;
				return;
			}
		}
		catch (InvalidOperationException)
		{
			// Falls back to inferred type below.
		}

		command.Parameters.AddWithValue(source.ParameterName, source.Value);
	}
}
