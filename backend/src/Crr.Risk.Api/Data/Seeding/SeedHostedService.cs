using Microsoft.Extensions.Options;

namespace Crr.Risk.Api.Data.Seeding;

public sealed class SeedHostedService : IHostedService
{
	private readonly CsvSeeder _csvSeeder;
	private readonly IOptions<SeedOptions> _seedOptions;
	private readonly ILogger<SeedHostedService> _logger;

	public SeedHostedService(
		CsvSeeder csvSeeder,
		IOptions<SeedOptions> seedOptions,
		ILogger<SeedHostedService> logger)
	{
		_csvSeeder = csvSeeder;
		_seedOptions = seedOptions;
		_logger = logger;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		if (!_seedOptions.Value.Enabled)
		{
			_logger.LogInformation("CSV seeding is disabled by configuration.");
			return;
		}

		await _csvSeeder.SeedAsync(cancellationToken);
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
