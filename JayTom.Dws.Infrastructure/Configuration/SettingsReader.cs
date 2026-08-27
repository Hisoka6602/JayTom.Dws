using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;

namespace JayTom.Dws.Infrastructure.Configuration;

/// <summary>
/// 基于本地配置仓储实现应用层只读配置边界。
/// </summary>
public sealed class SettingsReader : ISettingsReader {
    /// <summary>本地配置仓储。</summary>
    private readonly IConfigRepository _repository;

    /// <summary>
    /// 初始化配置读取适配器。
    /// </summary>
    /// <param name="repository">本地配置仓储。</param>
    public SettingsReader(IConfigRepository repository) {
        _repository = repository;
    }

    /// <summary>按键读取指定类型的配置对象。</summary>
    public Task<TSettings?> GetAsync<TSettings>(string key, CancellationToken cancellationToken = default)
        where TSettings : class {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _repository.FirstOrDefaultEntity<TSettings>(key, cancellationToken);
    }

    /// <summary>按键读取未经反序列化的原始配置值。</summary>
    public async Task<string?> GetRawAsync(
        string key,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var value = await _repository.FirstOrDefaultJsonEntity(key, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
