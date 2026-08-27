using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Storage;

/// <summary>将声音、图标和图像内容从关系数据库外置到受控文件存储。</summary>
public interface IBinaryAssetStore {
    /// <summary>原子保存资源并返回稳定引用。</summary>
    Task<OperationResult<BinaryAssetReference>> SaveAsync(
        string category,
        string fileName,
        Stream content,
        CancellationToken cancellationToken);

    /// <summary>在大小上限内读取资源。</summary>
    Task<OperationResult<byte[]>> ReadAsync(
        BinaryAssetReference reference,
        int maximumBytes,
        CancellationToken cancellationToken);

    /// <summary>删除资源；不存在时返回成功的 false。</summary>
    Task<OperationResult<bool>> DeleteAsync(
        BinaryAssetReference reference,
        CancellationToken cancellationToken);
}
