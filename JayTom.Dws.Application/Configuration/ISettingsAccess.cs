using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Configuration;

/// <summary>提供具有取消和显式错误结果的应用配置访问边界。</summary>
public interface ISettingsAccess
{
    /// <summary>读取并反序列化指定模块的配置。</summary>
    /// <typeparam name="TSettings">配置类型。</typeparam>
    /// <param name="key">模块配置键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>成功时携带配置；失败时携带稳定错误。</returns>
    Task<OperationResult<TSettings?>> ReadAsync<TSettings>(
        string key,
        CancellationToken cancellationToken = default)
        where TSettings : class;

    /// <summary>保存指定模块的配置。</summary>
    /// <typeparam name="TSettings">配置类型。</typeparam>
    /// <param name="key">模块配置键。</param>
    /// <param name="settings">待保存配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>显式保存结果。</returns>
    Task<Result> SaveAsync<TSettings>(
        string key,
        TSettings settings,
        CancellationToken cancellationToken = default);
}
