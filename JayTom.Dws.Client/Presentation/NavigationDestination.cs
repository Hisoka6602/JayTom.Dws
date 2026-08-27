using System;

namespace JayTom.Dws.Client.Presentation;

/// <summary>表示一个经过验证的 Prism 页面注册名称。</summary>
/// <param name="RegisteredName">页面注册名称。</param>
internal readonly record struct NavigationDestination(string RegisteredName)
{
    /// <summary>从页面注册名称创建目标标识。</summary>
    /// <param name="registeredName">页面注册名称。</param>
    /// <returns>强类型页面目标。</returns>
    public static NavigationDestination FromRegisteredName(string? registeredName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registeredName);
        if (Uri.TryCreate(registeredName, UriKind.Relative, out _) is false)
        {
            throw new ArgumentException("导航目标必须是相对注册名称。", nameof(registeredName));
        }

        return new NavigationDestination(registeredName);
    }

    /// <summary>将页面注册名称转换为 Prism 导航 URI。</summary>
    /// <returns>相对导航 URI。</returns>
    public Uri ToUri() => new(RegisteredName, UriKind.Relative);
}
