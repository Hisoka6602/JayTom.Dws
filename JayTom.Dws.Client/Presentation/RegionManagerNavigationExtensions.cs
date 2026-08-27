using System;
using Prism.Regions;

namespace JayTom.Dws.Client.Presentation;

/// <summary>为 Prism 区域管理器提供强类型导航入口。</summary>
internal static class RegionManagerNavigationExtensions
{
    /// <summary>按照强类型请求执行区域导航。</summary>
    /// <param name="regionManager">Prism 区域管理器。</param>
    /// <param name="request">导航请求。</param>
    public static void Navigate(this IRegionManager regionManager, NavigationRequest request)
    {
        ArgumentNullException.ThrowIfNull(regionManager);
        ArgumentNullException.ThrowIfNull(request);
        regionManager.Regions[request.Region.Name].RequestNavigate(request.Destination.ToUri());
    }
}
