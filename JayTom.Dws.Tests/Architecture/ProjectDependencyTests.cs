using System.Xml.Linq;
using NLog.Config;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>验证解决方案的项目依赖方向。</summary>
public sealed class ProjectDependencyTests {
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>验证内层项目不会引用被禁止的外层项目。</summary>
    [Theory]
    [InlineData("JayTom.Dws.Abstractions", "JayTom.Dws.Application")]
    [InlineData("JayTom.Dws.Abstractions", "JayTom.Dws.Client")]
    [InlineData("JayTom.Dws.Abstractions", "JayTom.Dws.Infrastructure")]
    [InlineData("JayTom.Dws.Abstractions", "JayTom.Dws.Domain")]
    [InlineData("JayTom.Dws.Abstractions", "JayTom.Dws.Plugin")]
    [InlineData("JayTom.Dws.Application", "JayTom.Dws.Client")]
    [InlineData("JayTom.Dws.Application", "JayTom.Dws.Infrastructure")]
    [InlineData("JayTom.Dws.Application", "JayTom.Dws.Interface")]
    [InlineData("JayTom.Dws.Application", "JayTom.Dws.Plugin")]
    [InlineData("JayTom.Dws.Application", "JayTom.Dws.Camera")]
    [InlineData("JayTom.Dws.Application", "JayTom.Dws.Ocr")]
    [InlineData("JayTom.Dws.Application", "JayTom.Dws.Nvr")]
    [InlineData("JayTom.Dws.Plugin.Abstractions", "JayTom.Dws.PluginInterface")]
    [InlineData("JayTom.Dws.Plugin.Abstractions", "JayTom.Dws.Client")]
    [InlineData("JayTom.Dws.Plugin.Abstractions", "JayTom.Dws.Infrastructure")]
    [InlineData("JayTom.Dws.Plugin.Abstractions", "JayTom.Dws.Interface")]
    [InlineData("JayTom.Dws.Domain", "JayTom.Dws.Interface")]
    [InlineData("JayTom.Dws.Domain", "JayTom.Dws.Plugin")]
    [InlineData("JayTom.Dws.Domain", "JayTom.Dws.Models")]
    [InlineData("JayTom.Dws.Domain", "JayTom.Dws.Legacy.Contracts")]
    [InlineData("JayTom.Dws.Interface", "JayTom.Dws.Plugin")]
    [InlineData("JayTom.Dws.Infrastructure", "JayTom.Dws.Plugin")]
    public void Inner_projects_must_not_reference_outer_projects(string projectName, string forbiddenReference) {
        var references = ReadProjectReferences(projectName);

        Assert.DoesNotContain(forbiddenReference, references, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>验证桌面端通过应用层边界组织用例。</summary>
    [Fact]
    public void Client_must_reference_the_application_boundary() {
        var references = ReadProjectReferences("JayTom.Dws.Client");

        Assert.Contains("JayTom.Dws.Application", references, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>验证核心插件契约不依赖桌面 UI、宿主或容器包。</summary>
    [Fact]
    public void Core_plugin_contract_must_remain_ui_and_host_independent() {
        var document = ReadProjectDocument("JayTom.Dws.Plugin.Abstractions");
        var targetFramework = document.Descendants("TargetFramework").First().Value;
        var packages = document.Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

        Assert.Equal(string.Concat("net10", ".", "0"), targetFramework);
        Assert.DoesNotContain(document.Descendants("UseWPF"), element =>
            string.Equals(element.Value, "true", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(packages, package =>
            package.Contains("Hosting", StringComparison.OrdinalIgnoreCase) ||
            package.Contains("DependencyInjection", StringComparison.OrdinalIgnoreCase) ||
            package.Contains("Drawing", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>验证 OCR 引擎不再启用 WPF 桌面框架。</summary>
    [Fact]
    public void Ocr_engine_must_not_enable_wpf() {
        var document = ReadProjectDocument("JayTom.Dws.Ocr");

        Assert.DoesNotContain(document.Descendants("UseWPF"), element =>
            string.Equals(element.Value, "true", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>验证日志洪峰不会通过阻塞型异步队列拖停业务线程，并限制单个日志文件大小。</summary>
    [Fact]
    public void Client_logging_must_apply_bounded_non_blocking_backpressure() {
        var configPath = Path.Combine(RepositoryRoot, "JayTom.Dws.Client", "Nlog.config");
        var document = XDocument.Load(configPath);
        var nlogNamespace = document.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var xsiNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
        var targets = document.Descendants(nlogNamespace + "target").ToArray();
        var asynchronousTargets = targets
            .Where(element => string.Equals(
                element.Attribute(xsiNamespace + "type")?.Value,
                "AsyncWrapper",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var fileTargets = targets
            .Where(element => string.Equals(
                element.Attribute(xsiNamespace + "type")?.Value,
                "File",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(asynchronousTargets);
        Assert.All(asynchronousTargets, target => {
            Assert.Equal("Discard", target.Attribute("overflowAction")?.Value);
            Assert.Equal("20000", target.Attribute("queueLimit")?.Value);
        });
        Assert.NotEmpty(fileTargets);
        Assert.All(fileTargets, target => {
            Assert.Equal("104857600", target.Attribute("archiveAboveSize")?.Value);
            Assert.Equal("30", target.Attribute("maxArchiveFiles")?.Value);
        });
        var parseException = Record.Exception(() =>
            new XmlLoggingConfiguration(configPath));
        Assert.Null(parseException);
    }

    /// <summary>读取指定项目的直接项目引用。</summary>
    private static IReadOnlyCollection<string> ReadProjectReferences(string projectName) {
        var document = ReadProjectDocument(projectName);

        return document.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFileNameWithoutExtension(path!))
            .ToArray();
    }

    /// <summary>读取指定项目文件。</summary>
    private static XDocument ReadProjectDocument(string projectName) {
        var projectPath = Path.Combine(RepositoryRoot, projectName, $"{projectName}.csproj");
        return XDocument.Load(projectPath);
    }

    /// <summary>从测试输出目录向上定位仓库根目录。</summary>
    private static string FindRepositoryRoot() {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "JayTom.Dws.sln"))) {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }
}
