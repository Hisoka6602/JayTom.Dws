using System.Text.Json;
using System.Xml.Linq;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>验证模块所有权、依赖治理和持续集成约束不会退化。</summary>
public sealed class EngineeringGovernanceTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>验证 CI 执行 Release 构建、全量测试、安全审计和质量守卫。</summary>
    [Fact]
    public void Continuous_integration_must_run_the_complete_release_gate()
    {
        string workflow = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            ".github",
            "workflows",
            "ci.yml"));

        Assert.Contains("dotnet restore .\\JayTom.Dws.sln", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet build .\\JayTom.Dws.sln -c Release", workflow, StringComparison.Ordinal);
        Assert.Contains("JayTom.Dws.CodeQualityGuard", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet test .\\JayTom.Dws.Tests\\JayTom.Dws.Tests.csproj -c Release", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet publish .\\JayTom.Dws.Client\\JayTom.Dws.Client.csproj -c Release", workflow, StringComparison.Ordinal);
        Assert.Contains("Test-PublishArtifact.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("NuGetAudit", File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Directory.Build.props")), StringComparison.Ordinal);
    }

    /// <summary>验证所有 NuGet 版本只由中央版本文件管理。</summary>
    [Fact]
    public void Dependency_versions_must_be_centrally_managed()
    {
        XDocument centralDocument = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "Directory.Packages.props"));
        HashSet<string> centralPackages = centralDocument
            .Descendants("PackageVersion")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string projectPath in Directory.EnumerateFiles(
                     RepositoryRoot,
                     "*.csproj",
                     SearchOption.AllDirectories)
                 .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))
        {
            foreach (XElement reference in XDocument.Load(projectPath).Descendants("PackageReference"))
            {
                string package = reference.Attribute("Include")?.Value
                    ?? throw new InvalidDataException($"PackageReference 缺少 Include: {projectPath}");
                Assert.Null(reference.Attribute("Version"));
                Assert.Null(reference.Element("Version"));
                Assert.Contains(package, centralPackages);
            }
        }
    }

    /// <summary>验证架构策略中的每个生产项目都具有明确模块所有者。</summary>
    [Fact]
    public void Every_production_project_must_have_a_module_owner()
    {
        string ownership = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "docs",
            "architecture",
            "module-ownership.md"));
        using JsonDocument policy = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "eng",
            "ArchitecturePolicy.json")));

        foreach (JsonProperty project in policy.RootElement
                     .GetProperty("projectReferences")
                     .EnumerateObject())
        {
            Assert.Contains($"| {project.Name} |", ownership, StringComparison.Ordinal);
        }
    }

    /// <summary>验证架构变更模板强制要求 ADR、迁移与测试证据。</summary>
    [Fact]
    public void Architecture_changes_must_request_decision_and_test_evidence()
    {
        string template = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            ".github",
            "pull_request_template.md"));

        Assert.Contains("docs/architecture/adr", template, StringComparison.Ordinal);
        Assert.Contains("[Obsolete]", template, StringComparison.Ordinal);
        Assert.Contains("单元、契约或架构测试", template, StringComparison.Ordinal);
        Assert.Contains("Release 构建、全量测试", template, StringComparison.Ordinal);
    }

    /// <summary>技术债预算必须同时进入本地构建、CI 和变更评审清单。</summary>
    [Fact]
    public void Technical_debt_budget_is_an_iteration_gate()
    {
        string workflow = File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "ci.yml"));
        string clientProject = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Client",
            "JayTom.Dws.Client.csproj"));
        string template = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            ".github",
            "pull_request_template.md"));

        Assert.Contains("CodeQualityBaseline.json", workflow, StringComparison.Ordinal);
        Assert.Contains("RunCodeQualityGuard", clientProject, StringComparison.Ordinal);
        Assert.Contains("未提高 `eng/CodeQualityBaseline.json`", template, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(
            RepositoryRoot,
            "docs",
            "architecture",
            "technical-debt-budget.md")));
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
               ?? throw new DirectoryNotFoundException("无法定位仓库根目录。");
    }
}
