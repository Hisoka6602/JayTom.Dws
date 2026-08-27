namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定业务事件与桌面 UI 事件的物理所有权。</summary>
public sealed class EventOwnershipArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>Domain 不得继续充当跨层事件杂物箱。</summary>
    [Fact]
    public void Domain_contains_no_legacy_event_contract_bundle()
    {
        string legacyPath = Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Domain",
            "EventMediators",
            "EventAggregator.cs");
        Assert.False(File.Exists(legacyPath));
    }

    /// <summary>业务事件归应用层，窗口动作归桌面表现层。</summary>
    [Fact]
    public void Business_and_window_events_have_distinct_owner_layers()
    {
        string applicationEvents = Path.Combine(RepositoryRoot, "JayTom.Dws.Application", "Events");
        string clientEvents = Path.Combine(RepositoryRoot, "JayTom.Dws.Client", "Events");

        Assert.True(File.Exists(Path.Combine(applicationEvents, "PackageExitUpdateEvent.cs")));
        Assert.True(File.Exists(Path.Combine(applicationEvents, "SettingsChangedEvent.cs")));
        Assert.True(File.Exists(Path.Combine(clientEvents, "WindowsAction.cs")));
        Assert.False(File.Exists(Path.Combine(applicationEvents, "WindowsAction.cs")));
    }

    /// <summary>每个事件契约必须独占源文件，避免再次聚合为巨型定义文件。</summary>
    [Fact]
    public void Event_contracts_are_isolated_by_file()
    {
        string[] files = Directory.EnumerateFiles(
                Path.Combine(RepositoryRoot, "JayTom.Dws.Application", "Events"),
                "*.cs")
            .Concat(Directory.EnumerateFiles(
                Path.Combine(RepositoryRoot, "JayTom.Dws.Client", "Events"),
                "*.cs"))
            .ToArray();

        Assert.True(files.Length >= 16);
        Assert.All(files, path => Assert.True(CountPublicTypes(File.ReadAllText(path)) == 1));
    }

    /// <summary>统计源文件中的公开类型声明。</summary>
    private static int CountPublicTypes(string source) =>
        new[] { "public sealed class ", "public enum ", "public sealed record " }
            .Sum(marker => source.Split(marker, StringSplitOptions.None).Length - 1);

    /// <summary>从测试输出目录向上定位仓库根目录。</summary>
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
