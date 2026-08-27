using System;

namespace JayTom.Dws.Client.Presentation;

/// <summary>表示一个经过验证的 Prism 区域名称。</summary>
/// <param name="Name">区域注册名称。</param>
internal readonly record struct NavigationRegion(string Name)
{
    /// <summary>从注册名称创建区域标识。</summary>
    /// <param name="name">区域注册名称。</param>
    /// <returns>强类型区域标识。</returns>
    public static NavigationRegion FromName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new NavigationRegion(name);
    }
}
