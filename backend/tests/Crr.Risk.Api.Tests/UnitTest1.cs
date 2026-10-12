using Crr.Risk.Api.Models.Dtos;
using Crr.Risk.Api.Querying;
using Crr.Risk.Api.Validation;
using System.Text.Json;

namespace Crr.Risk.Api.Tests;

public class RowsRequestValidatorTests
{
    private readonly RowsRequestValidator _validator = new(new AgGridFilterParser());

    [Fact]
    public void CreateForRows_Throws_WhenStartRowIsNegative()
    {
        var request = new RowsRequestDto
        {
            StartRow = -1,
            EndRow = 10,
            FilterModel = EmptyFilterModel(),
        };

        var action = () => _validator.CreateForRows(request);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("startRow must be >= 0.", exception.Message);
    }

    [Fact]
    public void CreateForRows_Throws_WhenEndRowIsNotGreaterThanStartRow()
    {
        var request = new RowsRequestDto
        {
            StartRow = 20,
            EndRow = 20,
            FilterModel = EmptyFilterModel(),
        };

        var action = () => _validator.CreateForRows(request);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("endRow must be greater than startRow.", exception.Message);
    }

    [Fact]
    public void CreateForRows_Throws_WhenPageSizeExceedsMaximum()
    {
        var request = new RowsRequestDto
        {
            StartRow = 0,
            EndRow = 501,
            FilterModel = EmptyFilterModel(),
        };

        var action = () => _validator.CreateForRows(request);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("Requested page size exceeds maximum of 500.", exception.Message);
    }

    [Fact]
    public void CreateForRows_ReturnsNormalizedCriteria_ForValidRequest()
    {
        var request = new RowsRequestDto
        {
            StartRow = 100,
            EndRow = 200,
            Search = "  copper  ",
            FilterModel = EmptyFilterModel(),
            SortModel =
            [
                new SortModelDto { ColId = "book", Sort = "asc" },
                new SortModelDto { ColId = "pnl", Sort = "desc" },
            ],
        };

        var criteria = _validator.CreateForRows(request);

        Assert.True(criteria.UsePaging);
        Assert.Equal(100, criteria.StartRow);
        Assert.Equal(200, criteria.EndRow);
        Assert.Equal("copper", criteria.Search);
        Assert.Collection(criteria.Sorts,
            sort =>
            {
                Assert.Equal("book", sort.ColumnId);
                Assert.False(sort.Descending);
            },
            sort =>
            {
                Assert.Equal("pnl", sort.ColumnId);
                Assert.True(sort.Descending);
            });
    }

    [Fact]
    public void CreateForAggregation_DisablesPaging_AndTrimsSearch()
    {
        var request = new AggregationRequestDto
        {
            GroupBy = "book",
            Search = "  acme  ",
            FilterModel = EmptyFilterModel(),
        };

        var criteria = _validator.CreateForAggregation(request);

        Assert.False(criteria.UsePaging);
        Assert.Equal(0, criteria.StartRow);
        Assert.Equal(0, criteria.EndRow);
        Assert.Equal("acme", criteria.Search);
    }

    private static JsonElement EmptyFilterModel()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }
}