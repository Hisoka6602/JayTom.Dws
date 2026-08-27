using JayTom.Dws.Abstractions.Integrations;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Integrations;
using System;

namespace JayTom.Dws.Client.Extensions;

/// <summary>
/// 为上传提供商注册表提供强类型解析辅助方法。
/// </summary>
internal static class UploadProviderRegistryExtensions {
    /// <summary>
    /// 创建并校验指定 API 类型的强类型提供商。
    /// </summary>
    public static TProvider Resolve<TProvider>(
        this IProviderRegistry<IDataUploader> registry,
        ApiType apiType)
        where TProvider : class, IDataUploader {
        ArgumentNullException.ThrowIfNull(registry);
        if (registry.TryResolve(apiType.ToString(), out var provider) &&
            provider is TProvider typedProvider) {
            return typedProvider;
        }

        throw new InvalidOperationException(
            $"上传提供商 {apiType} 未注册或类型不匹配: {typeof(TProvider).Name}");
    }
}
