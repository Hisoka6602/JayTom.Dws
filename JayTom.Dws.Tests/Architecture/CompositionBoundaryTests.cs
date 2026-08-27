namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定 Client 组合根与 Infrastructure 具体实现之间的依赖方向。</summary>
public sealed class CompositionBoundaryTests
{
    /// <summary>平台具体实现必须在 Infrastructure 内绑定，Client 只调用模块注册入口。</summary>
    [Fact]
    public void Platform_implementations_are_bound_by_infrastructure_module()
    {
        string repositoryRoot = FindRepositoryRoot();
        string clientRegistration = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "JayTom.Dws.Client",
            "Composition",
            "PlatformAdapterRegistration.cs"));
        string infrastructureRegistration = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "JayTom.Dws.Infrastructure",
            "DependencyInjection",
            "PlatformInfrastructureServiceCollectionExtensions.cs"));

        Assert.Contains("AddDwsInfrastructurePlatformAdapters()", clientRegistration, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSingleton<IComputer, Computer>", clientRegistration, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSingleton<ICloudApiClientMessageHub, CloudApiClientMessageHub>", clientRegistration, StringComparison.Ordinal);
        Assert.Contains("Infrastructure.IComputer.Computer", infrastructureRegistration, StringComparison.Ordinal);
        Assert.Contains("ICloudApiClientMessageHub, CloudApiClientMessageHub", infrastructureRegistration, StringComparison.Ordinal);
    }

    /// <summary>ViewModel 只能依赖应用目录或能力端口，不得引用 Infrastructure 命名空间和旧仓储接口。</summary>
    [Fact]
    public void ViewModels_depend_on_application_and_capability_ports()
    {
        string repositoryRoot = FindRepositoryRoot();
        string[] repositoryContracts = Directory.EnumerateFiles(
                Path.Combine(repositoryRoot, "JayTom.Dws.Legacy.Contracts", "Repository"),
                "*.cs",
                SearchOption.AllDirectories)
            .SelectMany(path => System.Text.RegularExpressions.Regex.Matches(
                    File.ReadAllText(path),
                    @"\binterface\s+(I[A-Za-z0-9_]+)\b")
                .Select(match => match.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        string[] viewModels = Directory.EnumerateFiles(
                Path.Combine(repositoryRoot, "JayTom.Dws.Client", "ViewModels"),
                "*.cs",
                SearchOption.AllDirectories)
            .ToArray();

        Assert.All(viewModels, path =>
        {
            string source = File.ReadAllText(path);
            Assert.DoesNotContain("using JayTom.Dws.Infrastructure", source, StringComparison.Ordinal);
            Assert.All(repositoryContracts, contract =>
                Assert.DoesNotMatch($@"\b{System.Text.RegularExpressions.Regex.Escape(contract)}\b", source));
        });

        string cacheViewModel = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "JayTom.Dws.Client",
            "ViewModels",
            "Pages",
            "Preferences",
            "CacheClearSettingsPageViewModel.cs"));
        Assert.Contains("IDiskInventory", cacheViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("IComputer", cacheViewModel, StringComparison.Ordinal);
    }

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
