namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定关键路径的统一结构化日志、关联标识、脱敏与指标接入。</summary>
public sealed class ObservabilityArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>启动、查询、命令和事件消费必须使用统一诊断入口。</summary>
    [Fact]
    public void Critical_paths_use_shared_diagnostics_and_correlation()
    {
        string lifecycle = Read("JayTom.Dws.Client", "Service", "Runtime", "ApplicationLifecycleCoordinator.cs");
        string command = Read("JayTom.Dws.Application", "Configuration", "MigrateConfigurationCommandHandler.cs");
        string query = Read("JayTom.Dws.Application", "PackageHistory", "SearchPackageHistoryQueryHandler.cs");
        string events = Read("JayTom.Dws.Application", "Messaging", "SequentialAsyncEventHandler.cs");

        foreach (string source in new[] { lifecycle, command, query, events })
        {
            Assert.Contains("DwsDiagnostics", source, StringComparison.Ordinal);
            Assert.Contains("CorrelationContext", source, StringComparison.Ordinal);
        }
    }

    /// <summary>API 循环异常不得再直接插值输出完整异常或外部响应。</summary>
    [Fact]
    public void Api_submit_logs_use_redaction_and_structured_fields()
    {
        string source = Read(
            "JayTom.Dws.Client",
            "Service",
            "BackgroundService",
            "SubmitApiBackgroundService.cs");

        Assert.Contains("ErrorSanitized(e, \"api-submit.loop\")", source, StringComparison.Ordinal);
        Assert.Contains("SensitiveDataRedactor.RedactMessage", source, StringComparison.Ordinal);
        Assert.Contains("{Operation}", source, StringComparison.Ordinal);
        Assert.Contains("{CorrelationId}", source, StringComparison.Ordinal);
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
