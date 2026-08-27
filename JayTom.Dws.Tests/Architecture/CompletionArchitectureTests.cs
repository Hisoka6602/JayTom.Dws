using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定资源包、实现可见性、资源所有权和关键测试矩阵。</summary>
public sealed class CompletionArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>验证大模型由可配置外置目录复制且清单中的长度与摘要真实有效。</summary>
    [Fact]
    public void Large_model_assets_are_externalized_and_integrity_registered()
    {
        string project = ReadSource("JayTom.Dws.Ocr", "JayTom.Dws.Ocr.csproj");
        Assert.Contains("DwsModelAssetsRoot", project, StringComparison.Ordinal);
        Assert.Contains("$(DwsModelAssetsRoot)\\OCR202311292106.onnx", project, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "EmbeddedResource Include=\"$(DwsModelAssetsRoot)",
            project,
            StringComparison.Ordinal);

        using JsonDocument manifest = JsonDocument.Parse(ReadSource("eng", "model-assets.json"));
        JsonElement package = manifest.RootElement.GetProperty("packages")[0];
        string assetPath = Path.Combine(
            RepositoryRoot,
            package.GetProperty("developmentCachePath").GetString()!
                .Replace('/', Path.DirectorySeparatorChar));
        var file = new FileInfo(assetPath);
        Assert.True(file.Exists);
        Assert.Equal(package.GetProperty("sizeBytes").GetInt64(), file.Length);
        using FileStream stream = File.OpenRead(assetPath);
        // DWS-HEX-COMPACT: SHA-256 清单格式要求连续十六进制摘要以便跨工具校验。
        string actualHash = Convert.ToHexString(SHA256.HashData(stream));
        Assert.Equal(package.GetProperty("sha256").GetString(), actualHash);
        Assert.Equal("external-copy", package.GetProperty("deployment").GetString());
    }

    /// <summary>验证生产实现项目的公共类数量不能超过审计基线。</summary>
    [Fact]
    public void Public_implementation_surface_does_not_grow()
    {
        using JsonDocument budget = JsonDocument.Parse(
            ReadSource("eng", "public-implementation-budget.json"));
        var publicClassPattern = new Regex(
            @"^\s*public\s+(?:sealed\s+|static\s+|abstract\s+|partial\s+)*class\s+",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);

        foreach (JsonProperty projectBudget in budget.RootElement.GetProperty("projects").EnumerateObject())
        {
            int actual = EnumerateSourceFiles(projectBudget.Name)
                .Sum(path => publicClassPattern.Matches(File.ReadAllText(path)).Count);
            Assert.True(
                actual <= projectBudget.Value.GetInt32(),
                $"{projectBudget.Name} public implementation classes grew to {actual}.");
        }

        foreach (string helper in new[]
                 {
                     Path.Combine("JayTom.Dws.Camera", "Testing", "SimulatedCamera.cs"),
                     Path.Combine("JayTom.Dws.Interface", "IntegrationResilienceHandler.cs"),
                     Path.Combine("JayTom.Dws.Interface", "IntegrationParameterSerializer.cs"),
                     Path.Combine("JayTom.Dws.Interface", "DefaultApiResponseEvaluator.cs")
                 })
        {
            Assert.Contains("internal", ReadSource(helper.Split(Path.DirectorySeparatorChar)), StringComparison.Ordinal);
        }
    }

    /// <summary>验证关键模块均登记了真实存在且包含测试方法的自动化测试文件。</summary>
    [Fact]
    public void Critical_modules_have_a_maintained_test_matrix()
    {
        using JsonDocument matrix = JsonDocument.Parse(ReadSource("eng", "critical-module-tests.json"));
        JsonProperty[] modules = matrix.RootElement.GetProperty("modules").EnumerateObject().ToArray();
        Assert.True(modules.Length >= 8);
        foreach (JsonProperty module in modules)
        {
            Assert.True(module.Value.GetArrayLength() > 0, $"{module.Name} has no tests.");
            foreach (JsonElement pathElement in module.Value.EnumerateArray())
            {
                string relativePath = pathElement.GetString()!;
                string path = Path.Combine(
                    RepositoryRoot,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(path), $"Missing critical test: {relativePath}");
                Assert.Contains("[Fact]", File.ReadAllText(path), StringComparison.Ordinal);
            }
        }
    }

    /// <summary>验证仓储并发锁使用共享占用句柄且旧的基础设施私有副本已移除。</summary>
    [Fact]
    public void Repository_semaphores_use_the_shared_idempotent_lease()
    {
        Assert.False(File.Exists(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Infrastructure",
            "SemaphoreLease.cs")));
        string[] repositoryBases =
        [
            ReadSource("JayTom.Dws.Infrastructure", "Repository", "RepositoryBase.cs"),
            ReadSource("JayTom.Dws.Infrastructure", "Repository", "LocalRepositoryBase.cs"),
            ReadSource("JayTom.Dws.Infrastructure", "Repository", "MemoryCacheRepositoryBase.cs")
        ];
        foreach (string source in repositoryBases)
        {
            Assert.Contains("Abstractions.Threading.SemaphoreLease.EnterAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain(".WaitAsync(_transactionSlim", source, StringComparison.Ordinal);
        }
        string lease = ReadSource("JayTom.Dws.Abstractions", "Threading", "SemaphoreLease.cs");
        Assert.Contains("Interlocked.Exchange", lease, StringComparison.Ordinal);
    }

    /// <summary>验证硬件与外部接口适配器均具备无真实依赖的契约测试。</summary>
    [Fact]
    public void Adapters_have_hardware_free_contract_tests()
    {
        string cameraTests = ReadSource("JayTom.Dws.Tests", "CameraAdapterContractTests.cs");
        string integrationTests = ReadSource("JayTom.Dws.Tests", "IntegrationBoundaryTests.cs");
        Assert.Contains("SimulatedCamera", cameraTests, StringComparison.Ordinal);
        Assert.Contains("StubHttpMessageHandler", integrationTests, StringComparison.Ordinal);
        Assert.Contains("cancellation", cameraTests, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sandbox.invalid", integrationTests, StringComparison.Ordinal);
    }

    /// <summary>枚举项目源文件并排除生成目录。</summary>
    private static IEnumerable<string> EnumerateSourceFiles(string projectName) =>
        Directory.EnumerateFiles(
            Path.Combine(RepositoryRoot, projectName),
            "*.cs",
            SearchOption.AllDirectories)
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                       !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    /// <summary>读取仓库中的文本文件。</summary>
    private static string ReadSource(params string[] pathParts) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot }.Concat(pathParts).ToArray()));

    /// <summary>定位仓库根目录。</summary>
    private static string FindRepositoryRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "JayTom.Dws.sln")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        throw new DirectoryNotFoundException("Cannot locate repository root.");
    }
}
