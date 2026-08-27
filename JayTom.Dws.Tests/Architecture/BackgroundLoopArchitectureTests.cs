namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定后台服务主循环必须响应宿主取消令牌。</summary>
public sealed class BackgroundLoopArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>包含循环的后台服务必须检查取消状态，且不得使用无限 while(true)。</summary>
    [Fact]
    public void Hosted_background_loops_are_cancellation_aware()
    {
        string[] roots =
        [
            Path.Combine(RepositoryRoot, "JayTom.Dws.Client", "Service", "BackgroundService"),
            Path.Combine(RepositoryRoot, "JayTom.Dws.Client", "Service", "ProcessingServices"),
            Path.Combine(RepositoryRoot, "JayTom.Dws.Client", "Service", "Runtime")
        ];

        string[] violations = roots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Select(path => (Path: path, Source: File.ReadAllText(path)))
            .Where(item => item.Source.Contains("while (", StringComparison.Ordinal) &&
                           ((!item.Source.Contains("CancellationRequested", StringComparison.Ordinal) &&
                             !item.Source.Contains("WaitForNextTickAsync(stoppingToken)", StringComparison.Ordinal)) ||
                            item.Source.Contains("while (true)", StringComparison.Ordinal)))
            .Select(item => Path.GetRelativePath(RepositoryRoot, item.Path))
            .ToArray();

        Assert.Empty(violations);
    }

    /// <summary>定位仓库根目录。</summary>
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "JayTom.Dws.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
