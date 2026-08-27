using JayTom.Dws.Application.Deployment;
using JayTom.Dws.Application.Storage;
using JayTom.Dws.Infrastructure.Storage;
using JayTom.Dws.Tests.TestDoubles;

namespace JayTom.Dws.Tests.Infrastructure;

/// <summary>验证数据库外二进制资源存储的原子读写与路径边界。</summary>
public sealed class FileBinaryAssetStoreTests {
    /// <summary>受控资源应通过不泄露物理路径的引用完成往返。</summary>
    [Fact]
    public async Task Asset_round_trips_through_stable_reference() {
        using var directory = TemporaryDirectory.Create("dws-asset-tests");
        var store = new FileBinaryAssetStore(new TestPathProvider(directory.Path));
        await using var input = new MemoryStream([1, 2, 3, 4], writable: false);

        var saved = await store.SaveAsync(
            "sounds",
            "notice.wav",
            input,
            CancellationToken.None);
        var loaded = await store.ReadAsync(
            saved.Value,
            1024,
            CancellationToken.None);

        Assert.True(saved.IsSuccess);
        Assert.True(loaded.IsSuccess);
        Assert.Equal([1, 2, 3, 4], loaded.Value);
        Assert.DoesNotContain(directory.Path, saved.Value.Value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>伪造引用和超过调用方上限的资源必须被拒绝。</summary>
    [Fact]
    public async Task Invalid_reference_and_read_limit_are_enforced() {
        using var directory = TemporaryDirectory.Create("dws-asset-tests");
        var store = new FileBinaryAssetStore(new TestPathProvider(directory.Path));
        await using var input = new MemoryStream(new byte[32], writable: false);
        var saved = await store.SaveAsync(
            "icons",
            "icon.png",
            input,
            CancellationToken.None);

        var escaped = await store.ReadAsync(
            new BinaryAssetReference("../outside/file.bin"),
            1024,
            CancellationToken.None);
        var limited = await store.ReadAsync(saved.Value, 8, CancellationToken.None);

        Assert.Equal("asset.reference_invalid", escaped.ErrorCode);
        Assert.Equal("asset.too_large", limited.ErrorCode);
    }

    private sealed class TestPathProvider : IApplicationPathProvider {
        public TestPathProvider(string root) {
            DataDirectory = Path.Combine(root, "data");
            ConfigurationDirectory = Path.Combine(root, "configuration");
            LogDirectory = Path.Combine(root, "logs");
            ModelDirectory = Path.Combine(root, "models");
            AdapterPackDirectory = Path.Combine(root, "adapters");
        }

        public string DataDirectory { get; }
        public string ConfigurationDirectory { get; }
        public string LogDirectory { get; }
        public string ModelDirectory { get; }
        public string AdapterPackDirectory { get; }
        public string GetDatabasePath(string databaseName) =>
            Path.Combine(DataDirectory, databaseName);
    }
}
