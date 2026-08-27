using System.Windows.Threading;

namespace JayTom.Dws.Client.Presentation;

/// <summary>
/// 集中封装 WPF 调度器访问，为展示层提供唯一的 UI 线程切换边界。
/// </summary>
internal static class UiThread
{
    /// <summary>获取应用程序调度器，并在设计时宿主中回退到当前调度器。</summary>
    public static Dispatcher Dispatcher =>
        System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
}
