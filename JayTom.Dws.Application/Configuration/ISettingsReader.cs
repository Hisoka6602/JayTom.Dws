namespace JayTom.Dws.Application.Configuration;

/// <summary>
/// 为应用用例提供只读配置访问，避免表现层直接依赖持久化仓储。
/// </summary>
public interface ISettingsReader {
    /// <summary>
    /// 按配置键读取原始值。
    /// </summary>
    /// <param name="key">配置键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>配置不存在时返回 <see langword="null"/>。</returns>
    Task<string?> GetRawAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按配置键读取并反序列化设置对象。
    /// </summary>
    /// <typeparam name="TSettings">设置对象类型。</typeparam>
    /// <param name="key">配置键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>存在且可解析时返回设置对象，否则返回 <see langword="null"/>。</returns>
    Task<TSettings?> GetAsync<TSettings>(string key, CancellationToken cancellationToken = default)
        where TSettings : class;
}
