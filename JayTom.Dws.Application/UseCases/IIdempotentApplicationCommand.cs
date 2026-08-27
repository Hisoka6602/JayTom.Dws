// DWS-COHESIVE-CONTRACTS: 幂等命令标记与幂等存储端口必须成对使用。
namespace JayTom.Dws.Application.UseCases;

/// <summary>标记可安全去重的应用命令。</summary>
public interface IIdempotentApplicationCommand<TResult> : IApplicationCommand<TResult> {
    /// <summary>获取调用方生成的稳定幂等键。</summary>
    string IdempotencyKey { get; }
}

/// <summary>持久化应用命令幂等状态。</summary>
public interface IApplicationIdempotencyStore {
    /// <summary>读取已完成命令的序列化结果。</summary>
    Task<string?> FindResultAsync(string key, CancellationToken cancellationToken);

    /// <summary>原子记录已完成命令的序列化结果。</summary>
    Task<bool> TryStoreResultAsync(
        string key,
        string serializedResult,
        CancellationToken cancellationToken);
}
