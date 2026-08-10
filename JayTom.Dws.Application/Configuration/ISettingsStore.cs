namespace JayTom.Dws.Application.Configuration;

/// <summary>
/// 提供与持久化技术无关的应用设置读写边界。
/// </summary>
public interface ISettingsStore : ISettingsReader {
    /// <summary>
    /// 判断是否已经存在任意应用配置。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>存在配置时返回 <see langword="true"/>。</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前配置的只读原始值快照。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按配置键组织的原始值快照。</returns>
    Task<IReadOnlyDictionary<string, string>> GetSnapshotAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 序列化并保存设置对象。
    /// </summary>
    /// <typeparam name="TSettings">设置对象类型。</typeparam>
    /// <param name="key">配置键。</param>
    /// <param name="settings">设置对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>保存成功时返回 <see langword="true"/>。</returns>
    Task<bool> SaveAsync<TSettings>(
        string key,
        TSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存无需 JSON 序列化的原始配置值。
    /// </summary>
    /// <param name="key">配置键。</param>
    /// <param name="value">原始配置值。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>保存成功时返回 <see langword="true"/>。</returns>
    Task<bool> SaveRawAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在同一持久化事务中保存多个原始配置值。
    /// </summary>
    /// <param name="values">按配置键组织的原始值。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>整批配置保存成功时返回 <see langword="true"/>。</returns>
    Task<bool> SaveRawBatchAsync(
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default);

}
