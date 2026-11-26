namespace BARANG.Core.Application.Pagination;

public sealed record PagedRequest
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public PagedRequest(int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaximumPageSize);

        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public int PageNumber { get; }

    public int PageSize { get; }
}
