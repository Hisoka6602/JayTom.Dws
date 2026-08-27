using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.Deployment;
using JayTom.Dws.Application.Storage;

namespace JayTom.Dws.Infrastructure.Storage;

/// <summary>在应用数据目录中以原子文件实现二进制资源存储。</summary>
internal sealed class FileBinaryAssetStore : IBinaryAssetStore {
    private const int AbsoluteMaximumBytes = 64 * 1024 * 1024;
    private readonly string _root;

    /// <summary>创建文件资源存储。</summary>
    public FileBinaryAssetStore(IApplicationPathProvider paths) {
        _root = Path.GetFullPath(Path.Combine(paths.DataDirectory, "assets"));
    }

    /// <inheritdoc />
    public async Task<OperationResult<BinaryAssetReference>> SaveAsync(
        string category,
        string fileName,
        Stream content,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(content);
        if (!IsSafeSegment(category) ||
            string.IsNullOrWhiteSpace(fileName) ||
            !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal)) {
            return OperationResult<BinaryAssetReference>.Failure(
                "asset.path_invalid",
                "资源分类或文件名格式无效。");
        }

        var extension = Path.GetExtension(fileName);
        var reference = new BinaryAssetReference(
            $"{category}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
        var path = Resolve(reference);
        var temporaryPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using (var target = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             64 * 1024,
                             FileOptions.Asynchronous | FileOptions.SequentialScan)) {
                var buffer = new byte[64 * 1024];
                var total = 0;
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0) {
                    total += read;
                    if (total > AbsoluteMaximumBytes) {
                        return OperationResult<BinaryAssetReference>.Failure(
                            "asset.too_large",
                            "资源超过允许的最大长度。");
                    }

                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }

            File.Move(temporaryPath, path, overwrite: false);
            return OperationResult<BinaryAssetReference>.Success(reference);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException) {
            return OperationResult<BinaryAssetReference>.Failure(
                "asset.write_failed",
                $"资源保存失败：{exception.Message}");
        }
        finally {
            if (File.Exists(temporaryPath)) {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <inheritdoc />
    public async Task<OperationResult<byte[]>> ReadAsync(
        BinaryAssetReference reference,
        int maximumBytes,
        CancellationToken cancellationToken) {
        if (maximumBytes is <= 0 or > AbsoluteMaximumBytes || !TryResolve(reference, out var path)) {
            return OperationResult<byte[]>.Failure("asset.reference_invalid", "资源引用无效。");
        }

        try {
            var file = new FileInfo(path);
            if (!file.Exists) {
                return OperationResult<byte[]>.Failure("asset.not_found", "资源不存在。");
            }

            if (file.Length > maximumBytes) {
                return OperationResult<byte[]>.Failure("asset.too_large", "资源超过读取上限。");
            }

            return OperationResult<byte[]>.Success(
                await File.ReadAllBytesAsync(path, cancellationToken));
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException) {
            return OperationResult<byte[]>.Failure(
                "asset.read_failed",
                $"资源读取失败：{exception.Message}");
        }
    }

    /// <inheritdoc />
    public Task<OperationResult<bool>> DeleteAsync(
        BinaryAssetReference reference,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryResolve(reference, out var path)) {
            return Task.FromResult(OperationResult<bool>.Failure(
                "asset.reference_invalid",
                "资源引用无效。"));
        }

        try {
            if (!File.Exists(path)) {
                return Task.FromResult(OperationResult<bool>.Success(false));
            }

            File.Delete(path);
            return Task.FromResult(OperationResult<bool>.Success(true));
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException) {
            return Task.FromResult(OperationResult<bool>.Failure(
                "asset.delete_failed",
                $"资源删除失败：{exception.Message}"));
        }
    }

    private string Resolve(BinaryAssetReference reference) =>
        TryResolve(reference, out var path)
            ? path
            : throw new ArgumentException("资源引用无效。", nameof(reference));

    private bool TryResolve(BinaryAssetReference reference, out string path) {
        path = string.Empty;
        var parts = reference.Value.Split('/');
        if (parts.Length != 2 || !IsSafeSegment(parts[0]) ||
            string.IsNullOrWhiteSpace(parts[1]) ||
            !string.Equals(parts[1], Path.GetFileName(parts[1]), StringComparison.Ordinal)) {
            return false;
        }

        var candidate = Path.GetFullPath(Path.Combine(_root, parts[0], parts[1]));
        var prefix = Path.TrimEndingDirectorySeparator(_root) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        path = candidate;
        return true;
    }

    private static bool IsSafeSegment(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 64 &&
        value.All(static character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
