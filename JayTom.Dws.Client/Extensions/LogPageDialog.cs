using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using MaterialDialogHost = MaterialDesignThemes.Wpf.DialogHost;

namespace JayTom.Dws.Client.Extensions;

/// <summary>日志页面只在宿主已加载时打开弹窗，并按本次会话关闭，避免切页及并发查询误关其他弹窗。</summary>
public sealed class LogPageDialog : IDisposable
{
    private readonly MaterialDialogHost? _host;
    private DialogSession? _session;
    private bool _disposed;

    private LogPageDialog(MaterialDialogHost? host, object content)
    {
        _host = host;
        if (host is null)
        {
            Completion = Task.CompletedTask;
            return;
        }
        host.Unloaded += HostUnloaded;
        Completion = ShowAsync(content);
    }

    /// <summary>打开弹窗并等待其结束；调用方必须等待或使用 Forget 观察异常。</summary>
    public Task Completion { get; }

    /// <summary>在 UI 线程查找已加载且空闲的页面宿主；尚未加载或已切走时跳过弹窗。</summary>
    public static LogPageDialog Open(object content, string identifier)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        var application = System.Windows.Application.Current;
        application?.Dispatcher.VerifyAccess();
        if (application is not null)
        {
            foreach (Window window in application.Windows)
            {
                if (FindHost(window, identifier) is { IsOpen: false } host)
                {
                    return new LogPageDialog(host, content);
                }
            }
        }
        return new LogPageDialog(null, content);
    }

    /// <summary>关闭自己打开的会话；宿主卸载、会话已关闭及重复调用均安全。</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _host?.Dispatcher.VerifyAccess();
        _disposed = true;
        if (_host is not null)
        {
            _host.Unloaded -= HostUnloaded;
        }
        if (_session is { IsEnded: false })
        {
            _session.Close();
        }
    }

    private async Task ShowAsync(object content)
    {
        try
        {
            await MaterialDialogHost.Show(content, _host!.Identifier!,
                new DialogOpenedEventHandler((_, args) =>
                {
                    _session = args.Session;
                    if (_disposed && !_session.IsEnded)
                    {
                        _session.Close();
                    }
                }));
        }
        finally
        {
            Dispose();
        }
    }

    private void HostUnloaded(object sender, RoutedEventArgs args) => Dispose();

    private static MaterialDialogHost? FindHost(DependencyObject root, string identifier)
    {
        if (root is MaterialDialogHost { IsLoaded: true } host && Equals(host.Identifier, identifier))
        {
            return host;
        }
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            if (FindHost(VisualTreeHelper.GetChild(root, index), identifier) is { } childHost)
            {
                return childHost;
            }
        }
        return null;
    }
}
