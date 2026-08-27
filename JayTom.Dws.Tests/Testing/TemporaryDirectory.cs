namespace JayTom.Dws.Tests.TestDoubles;

/// <summary>为测试创建并可靠清理独占临时目录。</summary>
internal sealed class TemporaryDirectory : IDisposable {
    private bool _disposed;

    private TemporaryDirectory(string path) {
        Path = path;
        Directory.CreateDirectory(path);
    }

    /// <summary>获取临时目录绝对路径。</summary>
    public string Path { get; }

    /// <summary>创建带稳定用途前缀的临时目录。</summary>
    public static TemporaryDirectory Create(string prefix) => new(System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        $"{prefix}-{Guid.NewGuid():N}"));

    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        if (Directory.Exists(Path)) {
            Directory.Delete(Path, recursive: true);
        }
    }
}
