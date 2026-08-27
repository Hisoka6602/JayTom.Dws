using System.Text.RegularExpressions;
using JayTom.Dws.Integrations;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定外部系统适配层的客户端、参数、解析、脱敏和沙箱边界。</summary>
public sealed class IntegrationArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>验证生产接口统一使用命名客户端且组合根不重复配置管道。</summary>
    [Fact]
    public void Integrations_use_the_central_named_http_client()
    {
        foreach (string path in EnumerateSourceFiles("JayTom.Dws.Interface"))
        {
            string source = File.ReadAllText(path);
            Assert.DoesNotContain(
                source.Split('\n'),
                line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal) &&
                        line.Contains("new HttpClient(", StringComparison.Ordinal));
            foreach (Match match in Regex.Matches(
                         source,
                         @"CreateClient\((?<argument>[^\)]*)\)",
                         RegexOptions.CultureInvariant))
            {
                Assert.Contains("ApiHttpClientNames.ExternalApi", match.Groups["argument"].Value, StringComparison.Ordinal);
            }
        }

        string composition = ReadSource(
            "JayTom.Dws.Client",
            "Composition",
            "PlatformAdapterRegistration.cs");
        Assert.Contains("AddDwsIntegrationHttpClient", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("AddHttpClient", composition, StringComparison.Ordinal);
    }

    /// <summary>验证超时、重试和熔断在唯一管道中声明并区分幂等请求。</summary>
    [Fact]
    public void Integration_resilience_policy_is_centralized()
    {
        string handler = ReadSource("JayTom.Dws.Interface", "IntegrationResilienceHandler.cs");
        string registration = ReadSource("JayTom.Dws.Interface", "IntegrationHttpClientRegistration.cs");

        Assert.Contains("RequestTimeout", handler, StringComparison.Ordinal);
        Assert.Contains("RetryAttempts", handler, StringComparison.Ordinal);
        Assert.Contains("CircuitFailureThreshold", handler, StringComparison.Ordinal);
        Assert.Contains("IsIdempotent", handler, StringComparison.Ordinal);
        Assert.Contains("AddHttpMessageHandler<IntegrationResilienceHandler>", registration, StringComparison.Ordinal);
    }

    /// <summary>验证公共基础参数采用不可变记录和值初始化语义。</summary>
    [Fact]
    public void External_parameter_contract_is_immutable_and_strongly_typed()
    {
        string source = ReadSource("JayTom.Dws.Integrations.Contracts", "BaseApiParameters.cs");
        Assert.Contains("record BaseApiParameters", source, StringComparison.Ordinal);
        Assert.DoesNotContain("get; set;", source, StringComparison.Ordinal);
        Assert.Equal(typeof(string), typeof(BaseApiParameters).GetProperty(nameof(BaseApiParameters.Url))?.PropertyType);
        Assert.Equal(typeof(int), typeof(BaseApiParameters).GetProperty(nameof(BaseApiParameters.TimeoutMilliseconds))?.PropertyType);
    }

    /// <summary>验证默认接口把响应业务判定委托给独立解析器。</summary>
    [Fact]
    public void Response_parsing_is_separated_from_transport_decisions()
    {
        string api = ReadSource("JayTom.Dws.Interface", "DefaultApi.cs");
        string evaluator = ReadSource("JayTom.Dws.Interface", "DefaultApiResponseEvaluator.cs");
        Assert.Contains("DefaultApiResponseEvaluator", api, StringComparison.Ordinal);
        Assert.Contains("_responseEvaluator.IsSuccess", api, StringComparison.Ordinal);
        Assert.Contains("Regex.IsMatch", evaluator, StringComparison.Ordinal);
        Assert.DoesNotContain("Regex.IsMatch", api, StringComparison.Ordinal);
    }

    /// <summary>验证接口审计快照不会绕过统一脱敏器。</summary>
    [Fact]
    public void Integration_credentials_do_not_enter_audit_logs()
    {
        foreach (string path in EnumerateSourceFiles("JayTom.Dws.Interface"))
        {
            string source = File.ReadAllText(path);
            Assert.DoesNotMatch(
                new Regex(
                    @"ApiParameters\s*=\s*JsonConvert\.SerializeObject",
                    RegexOptions.CultureInvariant),
                source);
        }
        Assert.Contains(
            "SensitiveDataRedactor",
            ReadSource("JayTom.Dws.Interface", "IntegrationParameterSerializer.cs"),
            StringComparison.Ordinal);
    }

    /// <summary>验证真实网络之外存在可重复的接口契约与内存沙箱测试。</summary>
    [Fact]
    public void External_systems_have_contract_and_sandbox_tests()
    {
        string tests = ReadSource("JayTom.Dws.Tests", "IntegrationBoundaryTests.cs");
        Assert.Contains("sandbox.invalid", tests, StringComparison.Ordinal);
        Assert.Contains("StubHttpMessageHandler", tests, StringComparison.Ordinal);
        Assert.Contains("Response_evaluator", tests, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClientHandler", tests, StringComparison.Ordinal);
    }

    /// <summary>枚举项目中的生产 C# 源文件。</summary>
    private static IEnumerable<string> EnumerateSourceFiles(string projectName) =>
        Directory.EnumerateFiles(
            Path.Combine(RepositoryRoot, projectName),
            "*.cs",
            SearchOption.AllDirectories)
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                       !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    /// <summary>读取指定项目中的源文件。</summary>
    private static string ReadSource(string projectName, params string[] pathParts) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot, projectName }.Concat(pathParts).ToArray()));

    /// <summary>从测试输出目录向上定位解决方案根目录。</summary>
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
