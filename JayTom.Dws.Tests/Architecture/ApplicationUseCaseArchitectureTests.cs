namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定应用用例与展示层之间的 Command、Query 和读模型边界。</summary>
public sealed class ApplicationUseCaseArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>启动流程只能通过应用命令执行配置迁移。</summary>
    [Fact]
    public void Lifecycle_uses_transactional_configuration_command()
    {
        string source = Read(
            "JayTom.Dws.Client",
            "Service",
            "Runtime",
            "ApplicationLifecycleCoordinator.cs");

        Assert.Contains("IApplicationCommandHandler<", source, StringComparison.Ordinal);
        Assert.Contains("new MigrateConfigurationCommand", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfigurationMigrationRunner _", source, StringComparison.Ordinal);
    }

    /// <summary>数据管理 ViewModel 只能使用应用查询处理器，不得接收 EF 包裹实体。</summary>
    [Fact]
    public void Data_management_uses_query_handler_and_detached_read_model()
    {
        string source = Read(
            "JayTom.Dws.Client",
            "ViewModels",
            "Pages",
            "Preferences",
            "DataManagementViewModel.cs");
        string contract = Read(
            "JayTom.Dws.Application",
            "PackageHistory",
            "IPackageHistoryQueryService.cs");

        Assert.Contains("IApplicationQueryHandler<SearchPackageHistoryQuery", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IPackageHistoryQueryService _packageHistory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PackageInfoModel", contract, StringComparison.Ordinal);
    }

    /// <summary>读取仓库内文件。</summary>
    private static string Read(params string[] segments) =>
        File.ReadAllText(Path.Combine([RepositoryRoot, .. segments]));

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
