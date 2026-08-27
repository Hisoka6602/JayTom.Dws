namespace JayTom.Dws.Client.Presentation;

/// <summary>集中保存应用内使用的 Prism 区域标识。</summary>
internal static class NavigationRegions
{
    /// <summary>主内容区域。</summary>
    public static NavigationRegion Content { get; } = NavigationRegion.FromName("ContentRegion");

    /// <summary>应用设置区域。</summary>
    public static NavigationRegion AppSettings { get; } = NavigationRegion.FromName("AppSettingsRegion");

    /// <summary>分拣设置区域。</summary>
    public static NavigationRegion PackageSorting { get; } = NavigationRegion.FromName("PackageSortingRegion");

    /// <summary>相机设置区域。</summary>
    public static NavigationRegion CameraConfiguration { get; } = NavigationRegion.FromName("CameraConfigRegion");

    /// <summary>云服务设置区域。</summary>
    public static NavigationRegion CloudService { get; } = NavigationRegion.FromName("CloudServiceRegion");
}
