using System.Text.Json;

namespace Crr.Risk.Api.Models.Dtos;

public sealed class SortModelDto
{
    public string ColId { get; init; } = string.Empty;
    public string Sort { get; init; } = string.Empty;
}

public sealed class RowsRequestDto
{
    public int StartRow { get; init; }
    public int EndRow { get; init; }
    public IReadOnlyList<SortModelDto> SortModel { get; init; } = [];
    public JsonElement FilterModel { get; init; }
    public string? Search { get; init; }
}

public sealed class AggregationRequestDto
{
    public string GroupBy { get; init; } = string.Empty;
    public IReadOnlyList<SortModelDto> SortModel { get; init; } = [];
    public JsonElement FilterModel { get; init; }
    public string? Search { get; init; }
}

public sealed class RiskPositionDto
{
    public required string TradeId { get; init; }
    public required string Book { get; init; }
    public required string Commodity { get; init; }
    public required string Counterparty { get; init; }
    public required string Region { get; init; }
    public required string InstrumentType { get; init; }
    public decimal Position { get; init; }
    public required string Currency { get; init; }
    public decimal MarketPrice { get; init; }
    public decimal Pnl { get; init; }
    public decimal FxExposure { get; init; }
    public DateOnly AsOfDate { get; init; }
}

public sealed class RowsResponseDto
{
    public required IReadOnlyList<RiskPositionDto> Rows { get; init; }
    public long LastRow { get; init; }
}

public sealed class AggregationGroupDto
{
    public required string GroupKey { get; init; }
    public long RowCount { get; init; }
    public decimal PnlTotal { get; init; }
    public decimal FxExposureTotal { get; init; }
    public decimal? PositionTotal { get; init; }
    public string? PositionNote { get; init; }
}

public sealed class AggregationTotalsDto
{
    public long RowCount { get; init; }
    public decimal PnlTotal { get; init; }
    public decimal FxExposureTotal { get; init; }
}

public sealed class AggregationResponseDto
{
    public required IReadOnlyList<AggregationGroupDto> Groups { get; init; }
    public required AggregationTotalsDto GrandTotals { get; init; }
    public string? PositionNote { get; init; }
}
