using System.Text.Json;
using JayTom.Dws.Application.Configuration;

namespace JayTom.Dws.Tests.Application;

/// <summary>通过原子字典替换模拟配置存储。</summary>
internal sealed class InMemorySettingsStore : ISettingsStore
{
    /// <summary>当前完整快照。</summary>
    private Dictionary<string, string> _snapshot;

    /// <summary>使用初始快照创建内存配置存储。</summary>
    public InMemorySettingsStore(IReadOnlyDictionary<string, string> snapshot) =>
        _snapshot = new Dictionary<string, string>(snapshot, StringComparer.Ordinal);

    /// <summary>获取当前只读快照。</summary>
    public IReadOnlyDictionary<string, string> Snapshot => _snapshot;

    /// <summary>获取完整快照替换次数。</summary>
    public int ReplaceCount { get; private set; }

    /// <summary>判断是否存在配置。</summary>
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshot.Count > 0);

    /// <summary>获取当前快照副本。</summary>
    public Task<IReadOnlyDictionary<string, string>> GetSnapshotAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(
            new Dictionary<string, string>(_snapshot, StringComparer.Ordinal));

    /// <summary>读取并反序列化配置。</summary>
    public Task<TSettings?> GetAsync<TSettings>(
        string key,
        CancellationToken cancellationToken = default)
        where TSettings : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_snapshot.TryGetValue(key, out string? value)
            ? JsonSerializer.Deserialize<TSettings>(value)
            : null);
    }

    /// <summary>序列化并保存配置。</summary>
    public Task<bool> SaveAsync<TSettings>(
        string key,
        TSettings settings,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return SaveRawAsync(key, JsonSerializer.Serialize(settings), cancellationToken);
    }

    /// <summary>保存单个原始值。</summary>
    public Task<bool> SaveRawAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        _snapshot[key] = value;
        return Task.FromResult(true);
    }

    /// <summary>保存一批原始值。</summary>
    public async Task<bool> SaveRawBatchAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        foreach (var pair in values)
        {
            await SaveRawAsync(pair.Key, pair.Value, cancellationToken);
        }
        return true;
    }

    /// <summary>原子替换完整配置快照。</summary>
    public Task<bool> ReplaceSnapshotAsync(
        IReadOnlyDictionary<string, string> snapshot,
        CancellationToken cancellationToken = default)
    {
        _snapshot = new Dictionary<string, string>(snapshot, StringComparer.Ordinal);
        ReplaceCount++;
        return Task.FromResult(true);
    }

    /// <summary>读取原始配置值。</summary>
    public Task<string?> GetRawAsync(
        string key,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshot.GetValueOrDefault(key));
}
