using System.Text.RegularExpressions;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定异步任务必须等待、拥有或通过统一入口观察异常的架构约束。</summary>
public sealed class AsyncTaskArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>生产代码不得使用同步任务等待，释放入口通过 TaskCleanup 观察异步清理。</summary>
    [Fact]
    public void Production_code_does_not_block_on_tasks()
    {
        string[] projectDirectories = Directory.EnumerateDirectories(RepositoryRoot, "JayTom.Dws.*")
            .Where(path => !path.EndsWith("JayTom.Dws.Tests", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var blockingWait = new System.Text.RegularExpressions.Regex(
            @"\.GetResult\s*\(\s*\)|\.Wait\s*\(\s*\)",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        string[] violations = projectDirectories
            .SelectMany(path => Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => blockingWait.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(RepositoryRoot, path))
            .ToArray();

        Assert.Empty(violations);
        Assert.True(File.Exists(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Abstractions",
            "Threading",
            "TaskCleanup.cs")));
    }

    /// <summary>编译配置不得再次豁免未等待任务警告。</summary>
    [Fact]
    public void Unawaited_task_warning_is_a_build_error()
    {
        string source = File.ReadAllText(Path.Combine(RepositoryRoot, "Directory.Build.props"));
        Match warningsNotAsErrors = Regex.Match(
            source,
            "<WarningsNotAsErrors>(?<value>.*?)</WarningsNotAsErrors>",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        Assert.True(warningsNotAsErrors.Success);
        Assert.DoesNotContain(
            "CS4014",
            warningsNotAsErrors.Groups["value"].Value,
            StringComparison.Ordinal);
    }

    /// <summary>客户端不得再用丢弃赋值绕过任务异常观察。</summary>
    [Fact]
    public void Client_fire_and_forget_tasks_use_the_observer()
    {
        string clientRoot = Path.Combine(RepositoryRoot, "JayTom.Dws.Client");
        Regex discardedTask = new(
            @"_\s*=\s*[^;\r\n]*(?:Async|Task|InvokeAsync)\s*\(",
            RegexOptions.CultureInvariant);

        string[] violations = Directory
            .EnumerateFiles(clientRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                           !path.EndsWith("DispatcherTaskExtensions.cs", StringComparison.Ordinal))
            .Where(path => discardedTask.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(RepositoryRoot, path))
            .ToArray();

        Assert.Empty(violations);
    }

    /// <summary>统一观察器必须等待任务并隔离取消、任务故障和日志故障。</summary>
    [Fact]
    public void Task_observer_catches_all_terminal_paths()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Client",
            "Extensions",
            "DispatcherTaskExtensions.cs"));

        Assert.Contains("await task.ConfigureAwait(false)", source, StringComparison.Ordinal);
        Assert.Contains("catch (OperationCanceledException)", source, StringComparison.Ordinal);
        Assert.Contains("catch (Exception exception)", source, StringComparison.Ordinal);
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
