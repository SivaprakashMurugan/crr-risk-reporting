namespace Crr.Risk.Api.Data.Seeding;

public sealed class SeedOptions
{
    public bool Enabled { get; init; } = true;
    public string CsvPath { get; init; } = string.Empty;
}
