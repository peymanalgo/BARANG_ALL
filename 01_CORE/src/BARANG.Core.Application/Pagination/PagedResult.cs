using System.Collections.ObjectModel;

namespace BARANG.Core.Application.Pagination;

public sealed class PagedResult<T>
{
    public PagedResult(IEnumerable<T> items, int pageNumber, int pageSize, long totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, PagedRequest.MaximumPageSize);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        Items = new ReadOnlyCollection<T>(items.ToArray());
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = totalCount == 0 ? 0 : (long)Math.Ceiling(totalCount / (double)pageSize);
    }

    public IReadOnlyCollection<T> Items { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public long TotalCount { get; }

    public long TotalPages { get; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

}
