namespace JayTom.Dws.Application.Presentation;

/// <summary>表示与 UI 框架无关的分页状态。</summary>
/// <param name="TotalItems">数据总数。</param>
/// <param name="PageSize">每页数量。</param>
/// <param name="CurrentPage">规范化后的当前页。</param>
/// <param name="TotalPages">总页数。</param>
public sealed record PaginationState(
    int TotalItems,
    int PageSize,
    int CurrentPage,
    int TotalPages)
{
    /// <summary>根据数据总数、页大小和期望页码创建分页状态。</summary>
    /// <param name="totalItems">数据总数。</param>
    /// <param name="pageSize">每页数量。</param>
    /// <param name="requestedPage">期望页码。</param>
    /// <returns>经过边界规范化的分页状态。</returns>
    public static PaginationState Create(int totalItems, int pageSize, int requestedPage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalItems);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        int totalPages = totalItems / pageSize + (totalItems % pageSize > 0 ? 1 : 0);
        int currentPage = totalPages == 0
            ? 1
            : Math.Clamp(requestedPage, 1, totalPages);

        return new PaginationState(totalItems, pageSize, currentPage, totalPages);
    }
}
