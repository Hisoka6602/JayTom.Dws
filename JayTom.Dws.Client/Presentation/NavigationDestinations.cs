namespace JayTom.Dws.Client.Presentation;

/// <summary>集中保存启动时使用的默认页面目标。</summary>
internal static class NavigationDestinations
{
    /// <summary>其他设置页。</summary>
    public static NavigationDestination OtherSettings { get; } = NavigationDestination.FromRegisteredName("OtherSettingsPage");

    /// <summary>包裹出口定义页。</summary>
    public static NavigationDestination PackageExitDefinition { get; } = NavigationDestination.FromRegisteredName("PackageExitDefinitionPage");

    /// <summary>相机发现页。</summary>
    public static NavigationDestination CameraFinder { get; } = NavigationDestination.FromRegisteredName("CameraFinderPage");

    /// <summary>云视频页。</summary>
    public static NavigationDestination CloudVideo { get; } = NavigationDestination.FromRegisteredName("CloudVideoPage");
}
