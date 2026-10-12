using System.Globalization;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Crr.Risk.Api.Data.Seeding;

public sealed class CsvSeeder
{
	private const string CreateSchemaSql = """
		CREATE EXTENSION IF NOT EXISTS pg_trgm;

		CREATE TABLE IF NOT EXISTS risk_positions (
			trade_id TEXT PRIMARY KEY,
			book TEXT NOT NULL,
			commodity TEXT NOT NULL,
			counterparty TEXT NOT NULL,
			region TEXT NOT NULL,
			instrument_type TEXT NOT NULL,
			position NUMERIC(20, 4) NOT NULL,
			currency TEXT NOT NULL,
			market_price NUMERIC(20, 4) NOT NULL,
			pnl NUMERIC(20, 4) NOT NULL,
			fx_exposure NUMERIC(20, 4) NOT NULL,
			as_of_date DATE NOT NULL
		);

		CREATE INDEX IF NOT EXISTS ix_risk_positions_book ON risk_positions (book);
		CREATE INDEX IF NOT EXISTS ix_risk_positions_commodity ON risk_positions (commodity);
		CREATE INDEX IF NOT EXISTS ix_risk_positions_region ON risk_positions (region);
		CREATE INDEX IF NOT EXISTS ix_risk_positions_instrument_type ON risk_positions (instrument_type);
		CREATE INDEX IF NOT EXISTS ix_risk_positions_as_of_date ON risk_positions (as_of_date);
		CREATE INDEX IF NOT EXISTS ix_risk_positions_counterparty_trgm ON risk_positions USING gin (counterparty gin_trgm_ops);
		CREATE INDEX IF NOT EXISTS ix_risk_positions_commodity_trgm ON risk_positions USING gin (commodity gin_trgm_ops);
		""";

	private readonly IConfiguration _configuration;
	private readonly IWebHostEnvironment _environment;
	private readonly SeedOptions _seedOptions;
	private readonly ILogger<CsvSeeder> _logger;

	public CsvSeeder(
		IConfiguration configuration,
		IWebHostEnvironment environment,
		IOptions<SeedOptions> seedOptions,
		ILogger<CsvSeeder> logger)
	{
		_configuration = configuration;
		_environment = environment;
		_seedOptions = seedOptions.Value;
		_logger = logger;
	}

	public async Task SeedAsync(CancellationToken cancellationToken)
	{
		var connectionString = _configuration.GetConnectionString("RiskDb");
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new InvalidOperationException("Connection string 'RiskDb' is not configured.");
		}

		await using var connection = new NpgsqlConnection(connectionString);
		await connection.OpenAsync(cancellationToken);

		await using (var createCommand = new NpgsqlCommand(CreateSchemaSql, connection))
		{
			await createCommand.ExecuteNonQueryAsync(cancellationToken);
		}

		var existingCount = await GetRowCountAsync(connection, cancellationToken);
		if (existingCount > 0)
		{
			_logger.LogInformation("Seed skipped because table already has {Count} rows.", existingCount);
			return;
		}

		var csvPath = ResolveCsvPath();
		if (!File.Exists(csvPath))
		{
			throw new FileNotFoundException($"CSV file not found at '{csvPath}'.");
		}

		_logger.LogInformation("Seeding CSV from {CsvPath}", csvPath);

		var insertedRows = 0;
		await using (var writer = await connection.BeginBinaryImportAsync(
			"COPY risk_positions (trade_id, book, commodity, counterparty, region, instrument_type, position, currency, market_price, pnl, fx_exposure, as_of_date) FROM STDIN (FORMAT BINARY)",
			cancellationToken))
		{
			foreach (var record in ParseCsvRecords(csvPath))
			{
				cancellationToken.ThrowIfCancellationRequested();

				await writer.StartRowAsync(cancellationToken);
				writer.Write(record.TradeId, NpgsqlDbType.Text);
				writer.Write(record.Book, NpgsqlDbType.Text);
				writer.Write(record.Commodity, NpgsqlDbType.Text);
				writer.Write(record.Counterparty, NpgsqlDbType.Text);
				writer.Write(record.Region, NpgsqlDbType.Text);
				writer.Write(record.InstrumentType, NpgsqlDbType.Text);
				writer.Write(record.Position, NpgsqlDbType.Numeric);
				writer.Write(record.Currency, NpgsqlDbType.Text);
				writer.Write(record.MarketPrice, NpgsqlDbType.Numeric);
				writer.Write(record.Pnl, NpgsqlDbType.Numeric);
				writer.Write(record.FxExposure, NpgsqlDbType.Numeric);
				writer.Write(record.AsOfDate, NpgsqlDbType.Date);
				insertedRows++;
			}

			await writer.CompleteAsync(cancellationToken);
		}

		var currentCount = await GetRowCountAsync(connection, cancellationToken);
		_logger.LogInformation("Seed completed. Inserted {InsertedRows} rows. Table row count is {CurrentCount}.", insertedRows, currentCount);
	}

	private static IEnumerable<RiskPositionCsvRecord> ParseCsvRecords(string csvPath)
	{
		using var reader = new StreamReader(csvPath);
		var headerLine = reader.ReadLine();
		if (string.IsNullOrWhiteSpace(headerLine))
		{
			yield break;
		}

		while (!reader.EndOfStream)
		{
			var line = reader.ReadLine();
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			var values = line.Split(',');
			if (values.Length != 12)
			{
				throw new InvalidOperationException($"Invalid CSV row with {values.Length} columns: {line}");
			}

			yield return new RiskPositionCsvRecord
			{
				TradeId = values[0],
				Book = values[1],
				Commodity = values[2],
				Counterparty = values[3],
				Region = values[4],
				InstrumentType = values[5],
				Position = decimal.Parse(values[6], CultureInfo.InvariantCulture),
				Currency = values[7],
				MarketPrice = decimal.Parse(values[8], CultureInfo.InvariantCulture),
				Pnl = decimal.Parse(values[9], CultureInfo.InvariantCulture),
				FxExposure = decimal.Parse(values[10], CultureInfo.InvariantCulture),
				AsOfDate = DateOnly.Parse(values[11], CultureInfo.InvariantCulture),
			};
		}
	}

	private async Task<long> GetRowCountAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
	{
		await using var countCommand = new NpgsqlCommand("SELECT COUNT(*) FROM risk_positions", connection);
		var scalar = await countCommand.ExecuteScalarAsync(cancellationToken);
		return scalar is long count ? count : Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
	}

	private string ResolveCsvPath()
	{
		if (!string.IsNullOrWhiteSpace(_seedOptions.CsvPath))
		{
			return Path.GetFullPath(_seedOptions.CsvPath, _environment.ContentRootPath);
		}

		return Path.GetFullPath(
			Path.Combine(_environment.ContentRootPath, "..", "..", "..", "data", "risk_positions.csv"));
	}

	public sealed class RiskPositionCsvRecord
	{
		public string TradeId { get; set; } = string.Empty;
		public string Book { get; set; } = string.Empty;
		public string Commodity { get; set; } = string.Empty;
		public string Counterparty { get; set; } = string.Empty;
		public string Region { get; set; } = string.Empty;
		public string InstrumentType { get; set; } = string.Empty;
		public decimal Position { get; set; }
		public string Currency { get; set; } = string.Empty;
		public decimal MarketPrice { get; set; }
		public decimal Pnl { get; set; }
		public decimal FxExposure { get; set; }
		public DateOnly AsOfDate { get; set; }
	}
}
