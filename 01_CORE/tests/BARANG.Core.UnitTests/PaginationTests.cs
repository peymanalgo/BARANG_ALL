using BARANG.Core.Application.Pagination;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void PagedRequestRejectsInvalidBoundaries(int pageNumber, int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedRequest(pageNumber, pageSize));
    }

    [Fact]
    public void PagedResultCalculatesPageMetadata()
    {
        PagedResult<int> result = new([21, 22], pageNumber: 2, pageSize: 20, totalCount: 41);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
        Assert.Equal(41, result.TotalCount);
    }

    [Fact]
    public void EmptyResultHasNoPagesOrItems()
    {
        PagedRequest request = new();
        PagedResult<string> result = new([], request.PageNumber, request.PageSize, 0);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasNextPage);
    }
}
