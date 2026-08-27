namespace JayTom.Dws.Client.Presentation;

/// <summary>封装一次区域导航所需的强类型区域与目标。</summary>
/// <param name="Region">目标区域。</param>
/// <param name="Destination">目标页面。</param>
internal sealed record NavigationRequest(
    NavigationRegion Region,
    NavigationDestination Destination)
{
    /// <summary>从区域和页面注册名称创建导航请求。</summary>
    /// <param name="region">目标区域。</param>
    /// <param name="registeredName">页面注册名称。</param>
    /// <returns>强类型导航请求。</returns>
    public static NavigationRequest To(NavigationRegion region, string? registeredName) =>
        new(region, NavigationDestination.FromRegisteredName(registeredName));
}
