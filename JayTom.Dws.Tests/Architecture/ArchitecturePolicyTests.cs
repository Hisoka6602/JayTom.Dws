using System.Text.Json;
using System.Xml.Linq;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>验证经过评审的完整项目依赖策略。</summary>
public sealed class ArchitecturePolicyTests {
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    /// <summary>架构策略。</summary>
    private static readonly ArchitecturePolicy Policy = ReadPolicy();

    /// <summary>验证项目引用图与允许列表完全一致。</summary>
    [Fact]
    public void Project_reference_graph_must_match_the_reviewed_allowlist() {
        foreach (var (projectName, expectedReferences) in Policy.ProjectReferences) {
            var actualReferences = ReadProjectDocument(projectName)
                .Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.GetFileNameWithoutExtension(path!))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.Equal(
                expectedReferences.Order(StringComparer.OrdinalIgnoreCase),
                actualReferences,
                StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>验证核心项目保持平台无关。</summary>
    [Fact]
    public void Core_projects_must_remain_platform_neutral() {
        foreach (var (projectName, expectedFramework) in Policy.TargetFrameworks) {
            var actualFramework = ReadProjectDocument(projectName)
                .Descendants("TargetFramework")
                .First()
                .Value;

            Assert.Equal(expectedFramework, actualFramework);
        }
    }

    /// <summary>验证核心项目未引入被禁止的包依赖。</summary>
    [Fact]
    public void Core_projects_must_not_take_forbidden_package_dependencies() {
        foreach (var (projectName, forbiddenPackages) in Policy.ForbiddenPackages) {
            var packages = ReadProjectDocument(projectName)
                .Descendants("PackageReference")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .ToArray();

            foreach (var forbiddenPackage in forbiddenPackages) {
                Assert.DoesNotContain(packages, package => package.Equals(
                    forbiddenPackage,
                    StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    /// <summary>验证 WPF 进程入口由主程序 Client 持有。</summary>
    [Fact]
    public void Wpf_process_entry_point_must_be_owned_by_client() {
        var clientOutputType = ReadProjectDocument("JayTom.Dws.Client")
            .Descendants("OutputType")
            .First()
            .Value;

        Assert.Equal("WinExe", clientOutputType);
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "JayTom.Dws.Client", "App.xaml")));
        Assert.False(File.Exists(Path.Combine(RepositoryRoot, "JayTom.Dws.Host.Wpf", "JayTom.Dws.Host.Wpf.csproj")));
    }

    /// <summary>读取架构策略。</summary>
    private static ArchitecturePolicy ReadPolicy() {
        var path = Path.Combine(RepositoryRoot, "eng", "ArchitecturePolicy.json");
        return JsonSerializer.Deserialize<ArchitecturePolicy>(
                   File.ReadAllText(path),
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidOperationException("Architecture policy is empty.");
    }

    /// <summary>读取指定项目的项目文件。</summary>
    private static XDocument ReadProjectDocument(string projectName) {
        var path = Path.Combine(RepositoryRoot, projectName, $"{projectName}.csproj");
        return XDocument.Load(path);
    }

    /// <summary>从测试输出目录向上定位仓库根目录。</summary>
    private static string FindRepositoryRoot() {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "JayTom.Dws.sln"))) {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
