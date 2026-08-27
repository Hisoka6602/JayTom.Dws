// DWS-COHESIVE-CONTRACTS: 版本信封、冲突策略和存储端口必须同步演进。
using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Configuration;

/// <summary>包装具有模式版本和并发版本的强类型配置。</summary>
public sealed record VersionedConfiguration<TValue>(
    int SchemaVersion,
    long Version,
    string ETag,
    TValue Value);

/// <summary>定义配置同步冲突的处理策略。</summary>
public enum ConfigurationConflictPolicy {
    /// <summary>发现版本冲突时拒绝覆盖。</summary>
    Reject,
    /// <summary>以本地显式修改为准。</summary>
    PreferLocal,
    /// <summary>以远端较新版本为准。</summary>
    PreferRemote,
    /// <summary>仅合并不冲突字段。</summary>
    MergeNonConflicting
}

/// <summary>持久化强类型、版本化配置且不泄漏数据库模型。</summary>
public interface IVersionedConfigurationStore {
    /// <summary>读取配置。</summary>
    Task<OperationResult<VersionedConfiguration<TValue>>> ReadAsync<TValue>(
        string section,
        CancellationToken cancellationToken);

    /// <summary>使用期望 ETag 乐观保存配置。</summary>
    Task<OperationResult<VersionedConfiguration<TValue>>> SaveAsync<TValue>(
        string section,
        int schemaVersion,
        TValue value,
        string? expectedETag,
        ConfigurationConflictPolicy conflictPolicy,
        CancellationToken cancellationToken);
}
