using JayTom.Dws.Application.Messaging;

namespace JayTom.Dws.Tests;

/// <summary>为本地存图测试提供无外部依赖的事件订阅边界，测试不启用 FTP。</summary>
internal sealed class SavedImageTestEventBus : IEventBus
{
    /// <summary>测试订阅生命周期。</summary>
    private sealed class Subscription : IDisposable
    {
        /// <summary>释放空订阅。</summary>
        public void Dispose() { }
    }
    /// <summary>当前测试不发布普通事件。</summary>
    public void Publish<TEvent>(TEvent eventData) { }
    /// <summary>接受存图设置事件订阅。</summary>
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) => new Subscription();
    /// <summary>接受异步设置事件订阅。</summary>
    public IDisposable SubscribeAsync<TEvent>(Func<TEvent, Task> handler) => new Subscription();
    /// <summary>当前测试不发布包裹事件。</summary>
    public void PublishPackage<TEvent>(TEvent eventData) { }
    /// <summary>接受包裹事件订阅。</summary>
    public IDisposable SubscribePackage<TEvent>(Action<TEvent> handler) => new Subscription();
    /// <summary>接受异步包裹事件订阅。</summary>
    public IDisposable SubscribePackageAsync<TEvent>(Func<TEvent, Task> handler) => new Subscription();
    /// <summary>释放普通订阅。</summary>
    public void Unsubscribe<TEvent>(Action<TEvent> handler) { }
    /// <summary>释放包裹订阅。</summary>
    public void UnsubscribePackage<TEvent>(Action<TEvent> handler) { }
}
