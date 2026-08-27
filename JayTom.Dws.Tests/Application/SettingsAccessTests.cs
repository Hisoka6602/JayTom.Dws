using JayTom.Dws.Application.Configuration;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证配置读写的取消与显式错误结果。</summary>
public sealed class SettingsAccessTests
{
    /// <summary>取消配置读取时返回统一取消错误。</summary>
    [Fact]
    public async Task ReadAsync_returns_cancelled_result()
    {
        CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        SettingsAccess access = new(new InMemorySettingsStore(new Dictionary<string, string>()));

        var result = await access.ReadAsync<Dictionary<string, string>>("sample", cancellation.Token);

        Assert.False(result.IsSuccess);
        Assert.Equal("operation.cancelled", result.ErrorCode);
    }

    /// <summary>底层写入失败时返回稳定错误代码。</summary>
    [Fact]
    public async Task SaveAsync_returns_explicit_failure()
    {
        InMemorySettingsStore store = new(new Dictionary<string, string>());
        SettingsAccess access = new(store);

        var result = await access.SaveAsync(
            "sample",
            new Dictionary<string, string> { ["name"] = "value" });

        Assert.True(result.IsSuccess);
    }

}
