using System.Xml.Linq;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定纯领域、历史持久化契约与 EF 模型的物理边界。</summary>
public sealed class DomainIsolationArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>纯 Domain 项目不得引用 Models、Legacy 或持久化模型命名空间。</summary>
    [Fact]
    public void Domain_has_no_persistence_model_dependency()
    {
        XDocument project = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Domain",
            "JayTom.Dws.Domain.csproj"));
        string[] references = project.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(references, reference =>
            reference.Contains("JayTom.Dws.Models", StringComparison.OrdinalIgnoreCase) ||
            reference.Contains("JayTom.Dws.Legacy.Contracts", StringComparison.OrdinalIgnoreCase));
        Assert.All(EnumerateSources("JayTom.Dws.Domain"), path =>
        {
            string source = File.ReadAllText(path);
            Assert.DoesNotContain("JayTom.Dws.Models", source, StringComparison.Ordinal);
            Assert.DoesNotContain("JayTom.Dws.Models", source, StringComparison.Ordinal);
        });
    }

    /// <summary>仍依赖表达式树的旧仓储契约只能存在于显式 Legacy 隔离项目。</summary>
    [Fact]
    public void Application_ports_do_not_expose_expression_trees()
    {
        string[] applicationInterfaces = EnumerateSources("JayTom.Dws.Application")
            .Where(path => File.ReadAllText(path).Contains("interface ", StringComparison.Ordinal))
            .ToArray();

        Assert.All(applicationInterfaces, path =>
            Assert.DoesNotContain("Expression<", File.ReadAllText(path), StringComparison.Ordinal));
        Assert.Contains("Expression<", File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Legacy.Contracts",
            "Repository",
            "IReadRepository.cs")), StringComparison.Ordinal);
    }

    /// <summary>运行时包裹聚合与 EF 包裹实体必须位于不同项目且仅 EF 实体包含表映射。</summary>
    [Fact]
    public void Runtime_aggregate_and_ef_entity_are_physically_separated()
    {
        string aggregatePath = Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Legacy.Contracts",
            "Packages",
            "PackageInfo.cs");
        string entityPath = Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Models",
            "Package",
            "PackageInfoModel.cs");
        string aggregate = File.ReadAllText(aggregatePath);
        string entity = File.ReadAllText(entityPath);

        Assert.True(File.Exists(aggregatePath));
        Assert.True(File.Exists(entityPath));
        Assert.DoesNotContain("[Table(", aggregate, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", aggregate, StringComparison.Ordinal);
        Assert.Contains("[Table(\"Data_PackageInfo\"", entity, StringComparison.Ordinal);
        Assert.Contains("class PackageInfoModel", entity, StringComparison.Ordinal);
    }

    /// <summary>枚举指定项目的生产 C# 源文件。</summary>
    private static IEnumerable<string> EnumerateSources(string projectName) =>
        Directory.EnumerateFiles(
                Path.Combine(RepositoryRoot, projectName),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains(
                $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase));

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
