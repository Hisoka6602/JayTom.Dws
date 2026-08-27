using System;
using System.Threading;

namespace JayTom.Dws.Client.EventMediators;

/// <summary>表示可重复安全释放的一次事件订阅。</summary>
internal sealed class EventSubscription : IDisposable
{
    /// <summary>释放订阅时执行的回调。</summary>
    private Action? _dispose;

    /// <summary>创建事件订阅释放句柄。</summary>
    public EventSubscription(Action dispose) => _dispose = dispose;

    /// <summary>取消订阅且保证释放动作最多执行一次。</summary>
    public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
}
