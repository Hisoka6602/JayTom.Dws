using JayTom.Dws.Application.Presentation;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证无 WPF 依赖的分页展示逻辑。</summary>
public sealed class PaginationStateTests
{
    /// <summary>不足整页的数据会计入最后一页。</summary>
    [Fact]
    public void Create_rounds_partial_page_up()
    {
        PaginationState state = PaginationState.Create(1001, 500, 2);

        Assert.Equal(3, state.TotalPages);
        Assert.Equal(2, state.CurrentPage);
    }

    /// <summary>请求页码会被约束到有效范围。</summary>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(8, 3)]
    public void Create_clamps_requested_page(int requestedPage, int expectedPage)
    {
        PaginationState state = PaginationState.Create(21, 10, requestedPage);

        Assert.Equal(expectedPage, state.CurrentPage);
    }

    /// <summary>空结果保持第一页但总页数为零。</summary>
    [Fact]
    public void Create_represents_empty_result()
    {
        PaginationState state = PaginationState.Create(0, 10, 20);

        Assert.Equal(0, state.TotalPages);
        Assert.Equal(1, state.CurrentPage);
    }
}
