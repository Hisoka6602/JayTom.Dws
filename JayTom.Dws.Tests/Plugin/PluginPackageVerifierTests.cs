using System.Security.Cryptography;
using JayTom.Dws.Plugin.Contracts;
using JayTom.Dws.Plugin.Runtime;
using JayTom.Dws.Tests.TestDoubles;

namespace JayTom.Dws.Tests.Plugin;

/// <summary>验证生产插件校验器的签名、摘要、权限与吊销边界。</summary>
public sealed class PluginPackageVerifierTests {
    /// <summary>受信密钥签发且权限获批的插件应通过校验。</summary>
    [Fact]
    public async Task Signed_package_with_allowed_permissions_is_trusted() {
        using var fixture = PluginSigningFixture.Create();
        var manifest = fixture.CreateSignedManifest(["camera.read"]);
        var verifier = fixture.CreateVerifier(new HashSet<string> { "camera.read" });

        var result = await verifier.VerifyAsync(
            manifest,
            fixture.AssemblyPath,
            CancellationToken.None);

        Assert.True(result.IsTrusted);
    }

    /// <summary>程序集篡改和未授权权限都必须被拒绝。</summary>
    [Fact]
    public async Task Tampering_and_permission_escalation_are_rejected() {
        using var fixture = PluginSigningFixture.Create();
        var manifest = fixture.CreateSignedManifest(["camera.admin"]);
        var verifier = fixture.CreateVerifier(new HashSet<string> { "camera.read" });

        var denied = await verifier.VerifyAsync(
            manifest,
            fixture.AssemblyPath,
            CancellationToken.None);
        await File.AppendAllTextAsync(fixture.AssemblyPath, "tampered");
        var tampered = await fixture.CreateVerifier(
            new HashSet<string> { "camera.admin" }).VerifyAsync(
            manifest,
            fixture.AssemblyPath,
            CancellationToken.None);

        Assert.False(denied.IsTrusted);
        Assert.False(tampered.IsTrusted);
    }

    /// <summary>吊销密钥和路径穿越密钥标识不得进入信任目录解析。</summary>
    [Fact]
    public async Task Revoked_and_invalid_key_identifiers_are_rejected() {
        using var fixture = PluginSigningFixture.Create();
        var revokedManifest = fixture.CreateSignedManifest([]);
        var revoked = await fixture.CreateVerifier(
            new HashSet<string>(),
            new HashSet<string> { fixture.KeyId }).VerifyAsync(
            revokedManifest,
            fixture.AssemblyPath,
            CancellationToken.None);
        var invalidManifest = fixture.CreateSignedManifest([], "../../outside");
        var invalid = await fixture.CreateVerifier(new HashSet<string>()).VerifyAsync(
            invalidManifest,
            fixture.AssemblyPath,
            CancellationToken.None);

        Assert.False(revoked.IsTrusted);
        Assert.False(invalid.IsTrusted);
    }

    private sealed class PluginSigningFixture : IDisposable {
        private readonly TemporaryDirectory _directory;
        private readonly RSA _rsa;

        private PluginSigningFixture(TemporaryDirectory directory, RSA rsa) {
            _directory = directory;
            _rsa = rsa;
            TrustDirectory = Path.Combine(directory.Path, "trust");
            Directory.CreateDirectory(TrustDirectory);
            AssemblyPath = Path.Combine(directory.Path, "plugin.dll");
            File.WriteAllBytes(AssemblyPath, RandomNumberGenerator.GetBytes(256));
            File.WriteAllText(
                Path.Combine(TrustDirectory, KeyId + ".pem"),
                rsa.ExportSubjectPublicKeyInfoPem());
        }

        public string AssemblyPath { get; }
        public string TrustDirectory { get; }
        public string KeyId { get; } = "test-key-01";

        public static PluginSigningFixture Create() => new(
            TemporaryDirectory.Create("dws-plugin-signing-tests"),
            RSA.Create(2048));

        public PluginPackageVerifier CreateVerifier(
            IReadOnlySet<string> permissions,
            IReadOnlySet<string>? revoked = null) => new(new PluginTrustOptions {
                TrustDirectory = TrustDirectory,
                AllowedPermissions = permissions,
                RevokedKeyIds = revoked ?? new HashSet<string>()
            });

        public PluginManifest CreateSignedManifest(
            IReadOnlyList<string> permissions,
            string? keyId = null) {
            var unsigned = CreateManifest(permissions, keyId ?? KeyId, string.Empty);
            var signature = Convert.ToBase64String(_rsa.SignData(
                System.Text.Encoding.UTF8.GetBytes(
                    PluginPackageVerifier.BuildSignaturePayload(unsigned)),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss));
            return CreateManifest(permissions, keyId ?? KeyId, signature);
        }

        public void Dispose() {
            _rsa.Dispose();
            _directory.Dispose();
        }

        private PluginManifest CreateManifest(
            IReadOnlyList<string> permissions,
            string keyId,
            string signature) {
            // DWS-HEX-COMPACT: 插件签名协议要求固定 64 位无分隔 SHA-256 摘要。
            return new PluginManifest {
                PluginKey = "jaytom.test",
                Name = "Test Plugin",
                Version = "1.0.0",
                MinimumHostVersion = "1.0.0",
                ContractMajorVersion = 1,
                EntryPoint = "plugin.dll::JayTom.Test.Plugin",
                Capabilities = ["camera"],
                Permissions = permissions,
                SigningKeyId = keyId,
                AssemblySha256 = Convert.ToHexString(SHA256.HashData(
                    File.ReadAllBytes(AssemblyPath))),
                Signature = signature
            };
        }
    }
}
