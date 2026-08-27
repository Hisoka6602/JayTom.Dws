using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.Configuration.Secrets;
using JayTom.Dws.Application.Deployment;

namespace JayTom.Dws.Infrastructure.Configuration;

/// <summary>使用 AES-256-GCM 保存独立秘密文件，密钥由外部提供器注入。</summary>
internal sealed class EncryptedFileSecretStore : ISecretStore {
    private const int MaximumSecretFileBytes = 64 * 1024;
    private readonly ISecretKeyProvider _keyProvider;
    private readonly string _directory;

    /// <summary>创建加密秘密存储。</summary>
    public EncryptedFileSecretStore(
        ISecretKeyProvider keyProvider,
        IApplicationPathProvider paths) {
        _keyProvider = keyProvider;
        _directory = Path.Combine(paths.ConfigurationDirectory, "secrets");
    }

    /// <inheritdoc />
    public async Task<OperationResult<SecretReference>> SetAsync(
        string purpose,
        ReadOnlyMemory<char> secret,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        if (secret.IsEmpty) {
            return OperationResult<SecretReference>.Failure(
                "secret.empty",
                "秘密值不能为空。");
        }

        var keyResult = await _keyProvider.GetKeyAsync(purpose, cancellationToken);
        if (!keyResult.IsSuccess || keyResult.Value is null) {
            return OperationResult<SecretReference>.Failure(keyResult.Error);
        }

        byte[] plaintext = Encoding.UTF8.GetBytes(secret.ToString());
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        try {
            using var aes = new AesGcm(keyResult.Value, tagSizeInBytes: tag.Length);
            aes.Encrypt(
                nonce,
                plaintext,
                ciphertext,
                tag,
                Encoding.UTF8.GetBytes(purpose));
            var envelope = new SecretEnvelope(
                1,
                purpose,
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(tag),
                Convert.ToBase64String(ciphertext));
            var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope);
            if (bytes.Length > MaximumSecretFileBytes) {
                return OperationResult<SecretReference>.Failure(
                    "secret.too_large",
                    "秘密值超过允许长度。");
            }

            Directory.CreateDirectory(_directory);
            var reference = new SecretReference(Guid.NewGuid().ToString("N"));
            await WriteAtomicallyAsync(GetPath(reference), bytes, cancellationToken);
            return OperationResult<SecretReference>.Success(reference);
        }
        catch (Exception exception) when (
            exception is CryptographicException or IOException or UnauthorizedAccessException) {
            return OperationResult<SecretReference>.Failure(
                "secret.write_failed",
                $"秘密保存失败：{exception.Message}");
        }
        finally {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(keyResult.Value);
        }
    }

    /// <inheritdoc />
    public async Task<OperationResult<string>> GetAsync(
        SecretReference reference,
        CancellationToken cancellationToken) {
        if (!Guid.TryParseExact(reference.Value, "N", out _)) {
            return OperationResult<string>.Failure(
                "secret.reference_invalid",
                "秘密引用格式无效。");
        }

        var path = GetPath(reference);
        try {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length is <= 0 or > MaximumSecretFileBytes) {
                return OperationResult<string>.Failure(
                    "secret.not_found",
                    "秘密引用不存在或内容长度无效。");
            }

            var envelope = JsonSerializer.Deserialize<SecretEnvelope>(
                await File.ReadAllBytesAsync(path, cancellationToken));
            if (envelope is null || envelope.FormatVersion != 1) {
                return OperationResult<string>.Failure(
                    "secret.format_invalid",
                    "秘密文件格式无效。");
            }

            var keyResult = await _keyProvider.GetKeyAsync(envelope.Purpose, cancellationToken);
            if (!keyResult.IsSuccess || keyResult.Value is null) {
                return OperationResult<string>.Failure(keyResult.Error);
            }

            var nonce = Convert.FromBase64String(envelope.Nonce);
            var tag = Convert.FromBase64String(envelope.Tag);
            var ciphertext = Convert.FromBase64String(envelope.Ciphertext);
            var plaintext = new byte[ciphertext.Length];
            try {
                using var aes = new AesGcm(keyResult.Value, tagSizeInBytes: tag.Length);
                aes.Decrypt(
                    nonce,
                    ciphertext,
                    tag,
                    plaintext,
                    Encoding.UTF8.GetBytes(envelope.Purpose));
                return OperationResult<string>.Success(Encoding.UTF8.GetString(plaintext));
            }
            finally {
                CryptographicOperations.ZeroMemory(plaintext);
                CryptographicOperations.ZeroMemory(keyResult.Value);
            }
        }
        catch (Exception exception) when (
            exception is CryptographicException or IOException or UnauthorizedAccessException or
            JsonException or FormatException) {
            return OperationResult<string>.Failure(
                "secret.read_failed",
                $"秘密读取失败：{exception.Message}");
        }
    }

    /// <inheritdoc />
    public Task<OperationResult<bool>> DeleteAsync(
        SecretReference reference,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Guid.TryParseExact(reference.Value, "N", out _)) {
            return Task.FromResult(OperationResult<bool>.Failure(
                "secret.reference_invalid",
                "秘密引用格式无效。"));
        }

        try {
            var path = GetPath(reference);
            if (!File.Exists(path)) {
                return Task.FromResult(OperationResult<bool>.Success(false));
            }

            File.Delete(path);
            return Task.FromResult(OperationResult<bool>.Success(true));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
            return Task.FromResult(OperationResult<bool>.Failure(
                "secret.delete_failed",
                $"秘密删除失败：{exception.Message}"));
        }
    }

    private string GetPath(SecretReference reference) {
        if (!Guid.TryParseExact(reference.Value, "N", out _)) {
            throw new ArgumentException("秘密引用格式无效。", nameof(reference));
        }

        return Path.Combine(_directory, reference.Value + ".secret");
    }

    private static async Task WriteAtomicallyAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken) {
        var temporaryPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try {
            await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally {
            if (File.Exists(temporaryPath)) {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed record SecretEnvelope(
        int FormatVersion,
        string Purpose,
        string Nonce,
        string Tag,
        string Ciphertext);
}
