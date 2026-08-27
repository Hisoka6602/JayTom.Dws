using System.Text.Json;
using System.Text.RegularExpressions;
using JayTom.Dws.Infrastructure.Jwt;
using JayTom.Dws.License;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>验证凭据、传输、SQL 与进程边界不会退回不安全实现。</summary>
public sealed class SecurityBoundaryTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>验证 JWT 验证始终启用签名、签发方、受众、期限与过期时间校验。</summary>
    [Fact]
    public void Jwt_validation_parameters_are_centralized_and_strict()
    {
        var settings = new TokenManagement
        {
            Secret = new string('s', 32),
            Issuer = "dws-tests",
            Audience = "dws-client",
            AccessExpiration = 10,
            RefreshExpiration = 60
        };

        var parameters = settings.CreateValidationParameters();

        Assert.True(parameters.ValidateIssuerSigningKey);
        Assert.True(parameters.RequireSignedTokens);
        Assert.True(parameters.ValidateIssuer);
        Assert.True(parameters.ValidateAudience);
        Assert.True(parameters.RequireExpirationTime);
        Assert.True(parameters.ValidateLifetime);
        Assert.Throws<InvalidOperationException>(() => new TokenManagement
        {
            Secret = "weak",
            Issuer = "issuer",
            Audience = "audience",
            AccessExpiration = 1,
            RefreshExpiration = 1
        }.Validate());
    }

    /// <summary>验证生产源码不绕过 TLS 证书校验，外部进程参数不使用拼接字符串。</summary>
    [Fact]
    public void Transport_and_process_boundaries_reject_unsafe_shortcuts()
    {
        foreach (string path in EnumerateProductionSourceFiles())
        {
            string source = File.ReadAllText(path);
            Assert.DoesNotContain(
                "DangerousAcceptAnyServerCertificateValidator",
                source,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "ServerCertificateCustomValidationCallback",
                source,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "RemoteCertificateValidationCallback",
                source,
                StringComparison.Ordinal);
            Assert.DoesNotMatch(
                new Regex(@"\.Arguments\s*=", RegexOptions.CultureInvariant),
                source);
        }

        string nvrSource = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Camera",
            "Cameras",
            "SecurityCamera",
            "DaHuatech",
            "NVR",
            "DaHuatechNVR.cs"));
        Assert.Contains("ArgumentList.Add", nvrSource, StringComparison.Ordinal);
    }

    /// <summary>验证接口模板不再携带真实密码、密钥或访问令牌。</summary>
    [Fact]
    public void Configuration_templates_do_not_contain_plaintext_credentials()
    {
        var credentialPattern = new Regex(
            "\\\"(?:Password|Secret|AppSecret|AccessToken|ApiKey|Ak|Sk|Key)\\\"\\s*:\\s*\\\"(?<value>[^\\\"]*)\\\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        string configurationDirectory = Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Interface",
            "ApiSettingJson");

        foreach (string path in Directory.EnumerateFiles(configurationDirectory, "*.json"))
        {
            string source = File.ReadAllText(path);
            foreach (Match match in credentialPattern.Matches(source))
            {
                string value = match.Groups["value"].Value;
                Assert.True(
                    string.IsNullOrWhiteSpace(value) || value.StartsWith("${", StringComparison.Ordinal),
                    $"配置模板 {Path.GetFileName(path)} 含明文凭据字段。" );
            }
        }
    }

    /// <summary>验证生产源码不包含密码、密钥、令牌或许可证的长字符串字面量。</summary>
    [Fact]
    public void Production_source_does_not_embed_plaintext_credentials()
    {
        var assignmentPattern = new Regex(
            "(?:Password|Secret|AppSecret|AccessToken|ApiKey|LicenseKey|PrivateKey)[A-Za-z0-9_]*\\s*=\\s*\\\"(?<value>[^\\\"]{8,})\\\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (string path in EnumerateProductionSourceFiles())
        {
            Match match = assignmentPattern.Match(File.ReadAllText(path));
            Assert.False(
                match.Success,
                $"生产源码 {Path.GetRelativePath(RepositoryRoot, path)} 含疑似明文凭据字面量。");
        }
    }

    /// <summary>验证生产授权文件生成后移除继承权限，仅保留生成账户、系统和管理员。</summary>
    [Fact]
    public void Production_license_must_apply_minimum_file_permissions()
    {
        string script = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "eng",
            "license",
            "New-DwsProductionLicense.ps1"));

        Assert.Contains("SetAccessRuleProtection($true, $false)", script, StringComparison.Ordinal);
        Assert.Contains("S-1-5-18", script, StringComparison.Ordinal);
        Assert.Contains("S-1-5-32-544", script, StringComparison.Ordinal);
        Assert.Contains("FileSystemRights]::Read", script, StringComparison.Ordinal);
        Assert.Contains("Set-Acl -LiteralPath $outputFullPath", script, StringComparison.Ordinal);
    }

    /// <summary>验证 GitHub 生产授权工作流完成私钥签发、公钥验签、信任根输出和临时密钥清理。</summary>
    [Fact]
    public void Production_license_workflow_closes_the_v2_signature_loop()
    {
        string workflow = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            ".github",
            "workflows",
            "production-license.yml"));
        string script = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "eng",
            "license",
            "New-DwsProductionLicense.ps1"));

        Assert.Contains("64 位 SHA-256 机器码", workflow, StringComparison.Ordinal);
        Assert.Contains("secrets.DWS_LICENSE_PRIVATE_KEY_PEM", workflow, StringComparison.Ordinal);
        Assert.Contains("secrets.DWS_LICENSE_PUBLIC_KEY_PEM", workflow, StringComparison.Ordinal);
        Assert.Contains("-PrivateKeyPath $privateKeyPath", workflow, StringComparison.Ordinal);
        Assert.Contains("-PublicKeyPath $publicKeyPath", workflow, StringComparison.Ordinal);
        Assert.Contains("Remove-Item -LiteralPath $temporaryKeyPath -Force", workflow, StringComparison.Ordinal);
        Assert.Contains("path: artifacts/license/**", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("32 位机器码", workflow, StringComparison.Ordinal);

        Assert.Contains("ValidatePattern('^[A-Fa-f0-9]{64}$')", script, StringComparison.Ordinal);
        Assert.Contains("'--private-key', $privateKeyFullPath", script, StringComparison.Ordinal);
        Assert.Contains("'--validate-file', $outputFullPath", script, StringComparison.Ordinal);
        Assert.Contains("'--public-key', $publicKeyFullPath", script, StringComparison.Ordinal);
        Assert.Contains("$envelope.algorithm -ne 'PS256'", script, StringComparison.Ordinal);
        Assert.Contains("Copy-Item -LiteralPath $publicKeyFullPath", script, StringComparison.Ordinal);
        Assert.Contains("license-manifest.json", script, StringComparison.Ordinal);
    }

    /// <summary>验证生产公钥随客户端发布，且文件名与公钥指纹一致。</summary>
    [Fact]
    public void Production_public_keys_are_packaged_as_application_trust_roots()
    {
        string trustDirectory = Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Client",
            "license-trust");
        string[] publicKeyPaths = Directory.GetFiles(trustDirectory, "*.pem");
        string project = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "JayTom.Dws.Client",
            "JayTom.Dws.Client.csproj"));

        Assert.NotEmpty(publicKeyPaths);
        foreach (string publicKeyPath in publicKeyPaths)
        {
            string expectedKeyId = Path.GetFileNameWithoutExtension(publicKeyPath);
            string actualKeyId = LicenseManager.ComputeKeyId(File.ReadAllText(publicKeyPath));
            Assert.Equal(expectedKeyId, actualKeyId);
        }

        Assert.Contains("<None Update=\"license-trust\\*.pem\">", project, StringComparison.Ordinal);
        Assert.Contains("<CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>", project, StringComparison.Ordinal);
    }

    /// <summary>验证通用仓储不再暴露可接收任意 SQL 的入口，Raw SQL 技术债为零。</summary>
    [Fact]
    public void Repository_boundaries_do_not_accept_raw_sql()
    {
        foreach (string fileName in new[] { "RepositoryBase.cs", "LocalRepositoryBase.cs" })
        {
            string source = File.ReadAllText(Path.Combine(
                RepositoryRoot,
                "JayTom.Dws.Infrastructure",
                "Repository",
                fileName));
            Assert.DoesNotContain("Task<int> ExecuteSqlAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Task<List<T>> FromSqlRaw", source, StringComparison.Ordinal);
        }

        using JsonDocument baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "eng",
            "CodeQualityBaseline.json")));
        JsonElement rawSql = baseline.RootElement.GetProperty("RawSql");
        Assert.Empty(rawSql.EnumerateObject());
    }

    /// <summary>枚举架构策略登记的生产项目源码。</summary>
    private static IEnumerable<string> EnumerateProductionSourceFiles()
    {
        using JsonDocument policy = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "eng",
            "ArchitecturePolicy.json")));
        foreach (JsonProperty project in policy.RootElement.GetProperty("projectReferences").EnumerateObject())
        {
            string projectDirectory = Path.Combine(RepositoryRoot, project.Name);
            foreach (string path in Directory.EnumerateFiles(
                         projectDirectory,
                         "*.cs",
                         SearchOption.AllDirectories))
            {
                if (!path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                    !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                {
                    yield return path;
                }
            }
        }
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
