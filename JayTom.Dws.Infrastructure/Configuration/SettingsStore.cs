using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Domain.Repository.LocalConf;
using Newtonsoft.Json;

namespace JayTom.Dws.Infrastructure.Configuration;

/// <summary>
/// 使用本地配置仓储实现应用设置的序列化与持久化。
/// </summary>
public sealed class SettingsStore : ISettingsStore {
    /// <summary>本地配置仓储。</summary>
    private readonly IConfigRepository _repository;

    /// <summary>
    /// 初始化设置存储适配器。
    /// </summary>
    /// <param name="repository">本地配置仓储。</param>
    public SettingsStore(IConfigRepository repository) {
        _repository = repository;
    }

    /// <summary>判断是否已存在任意应用配置。</summary>
    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default) {
        var items = await _repository.MemoryCacheData().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return items.Count > 0;
    }

    /// <summary>获取按配置键组织的原始值快照。</summary>
    public async Task<IReadOnlyDictionary<string, string>> GetSnapshotAsync(
        CancellationToken cancellationToken = default) {
        var items = await _repository.MemoryCacheData().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return items
            .GroupBy(item => item.ConfigName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Last().Value,
                StringComparer.Ordinal);
    }

    /// <summary>按键读取并反序列化设置对象。</summary>
    public Task<TSettings?> GetAsync<TSettings>(string key, CancellationToken cancellationToken = default)
        where TSettings : class {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _repository.FirstOrDefaultEntity<TSettings>(key, cancellationToken);
    }

    /// <summary>序列化并保存设置对象。</summary>
    public Task<bool> SaveAsync<TSettings>(
        string key,
        TSettings settings,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(settings);
        return SaveRawAsync(key, JsonConvert.SerializeObject(settings), cancellationToken);
    }

    /// <summary>保存原始配置值。</summary>
    public Task<bool> SaveRawAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        return _repository.InsertOrUpdate(new ConfigInfoModel {
            ConfigName = key,
            Value = value
        }, cancellationToken);
    }

    /// <summary>在单个仓储事务中保存多个原始配置值。</summary>
    public Task<bool> SaveRawBatchAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0) {
            return Task.FromResult(true);
        }

        var entities = values.Select(pair => {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
            ArgumentNullException.ThrowIfNull(pair.Value);
            return new ConfigInfoModel {
                ConfigName = pair.Key,
                Value = pair.Value
            };
        }).ToList();
        return _repository.InsertOrUpdateRange(entities, cancellationToken);
    }

    /// <summary>读取原始配置值。</summary>
    public async Task<string?> GetRawAsync(string key, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var value = await _repository.FirstOrDefaultJsonEntity(key, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
