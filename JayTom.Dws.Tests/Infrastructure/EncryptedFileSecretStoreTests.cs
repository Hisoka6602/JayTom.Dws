using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.Configuration.Secrets;
using JayTom.Dws.Application.Deployment;
using JayTom.Dws.Infrastructure.Configuration;
using JayTom.Dws.Tests.TestDoubles;

namespace JayTom.Dws.Tests.Infrastructure;

/// <summary>验证秘密存储的认证加密、篡改检测和引用边界。</summary>
public sealed class EncryptedFileSecretStoreTests {
    /// <summary>秘密明文只应通过有效引用完成往返。</summary>
    [Fact]
    public async Task Secret_round_trips_through_authenticated_envelope() {
        using var directory = TemporaryDirectory.Create("dws-secret-tests");
        var store = CreateStore(directory.Path);

        var saved = await store.SetAsync(
            "integration.api-key",
            "top-secret".AsMemory(),
            CancellationToken.None);
        var loaded = await store.GetAsync(saved.Value, CancellationToken.None);

        Assert.True(saved.IsSuccess);
        Assert.True(loaded.IsSuccess);
        Assert.Equal("top-secret", loaded.Value);
        Assert.DoesNotContain(
            "top-secret",
            await File.ReadAllTextAsync(Directory.GetFiles(
                Path.Combine(directory.Path, "configuration", "secrets"))[0]),
            StringComparison.Ordinal);
    }

    /// <summary>密文被篡改后认证标签必须阻止明文恢复。</summary>
    [Fact]
    public async Task Tampered_secret_is_rejected() {
        using var directory = TemporaryDirectory.Create("dws-secret-tests");
        var store = CreateStore(directory.Path);
        var saved = await store.SetAsync(
            "integration.password",
            "secret-value".AsMemory(),
            CancellationToken.None);
        var path = Directory.GetFiles(
            Path.Combine(directory.Path, "configuration", "secrets"))[0];
        var bytes = await File.ReadAllBytesAsync(path);
        bytes[^2] ^= 0x01;
        await File.WriteAllBytesAsync(path, bytes);

        var loaded = await store.GetAsync(saved.Value, CancellationToken.None);

        Assert.False(loaded.IsSuccess);
        Assert.Equal("secret.read_failed", loaded.ErrorCode);
    }

    /// <summary>伪造路径引用应返回结构化失败而不是访问文件系统边界之外。</summary>
    [Fact]
    public async Task Invalid_reference_is_rejected_without_path_resolution() {
        using var directory = TemporaryDirectory.Create("dws-secret-tests");
        var store = CreateStore(directory.Path);
        var reference = new SecretReference("../../outside");

        var loaded = await store.GetAsync(reference, CancellationToken.None);
        var deleted = await store.DeleteAsync(reference, CancellationToken.None);

        Assert.Equal("secret.reference_invalid", loaded.ErrorCode);
        Assert.Equal("secret.reference_invalid", deleted.ErrorCode);
    }

    private static EncryptedFileSecretStore CreateStore(string root) => new(
        new TestSecretKeyProvider(),
        new TestPathProvider(root));

    private sealed class TestSecretKeyProvider : ISecretKeyProvider {
        public ValueTask<OperationResult<byte[]>> GetKeyAsync(
            string purpose,
            CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(OperationResult<byte[]>.Success(
                Enumerable.Range(1, 32).Select(static value => (byte)value).ToArray()));
        }
    }

    private sealed class TestPathProvider : IApplicationPathProvider {
        public TestPathProvider(string root) {
            DataDirectory = Path.Combine(root, "data");
            ConfigurationDirectory = Path.Combine(root, "configuration");
            LogDirectory = Path.Combine(root, "logs");
            ModelDirectory = Path.Combine(root, "models");
            AdapterPackDirectory = Path.Combine(root, "adapters");
        }

        public string DataDirectory { get; }
        public string ConfigurationDirectory { get; }
        public string LogDirectory { get; }
        public string ModelDirectory { get; }
        public string AdapterPackDirectory { get; }
        public string GetDatabasePath(string databaseName) =>
            Path.Combine(DataDirectory, databaseName);
    }
}
