namespace JayTom.Dws.Application.Messaging;

/// <summary>定义应用内事件发布与订阅边界。</summary>
public interface IEventBus
{
    /// <summary>在发布线程同步调用普通订阅者；同一订阅者按发布顺序接收事件。</summary>
    void Publish<TEvent>(TEvent eventData);

    /// <summary>订阅普通应用事件。</summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler);

    /// <summary>
    /// 注册按发布顺序串行执行的异步订阅者；每个订阅者最多缓存 256 个待处理事件，超限时拒绝最新事件并记录错误。
    /// </summary>
    IDisposable SubscribeAsync<TEvent>(Func<TEvent, Task> handler);

    /// <summary>在发布线程同步调用高频包裹事件订阅者。</summary>
    void PublishPackage<TEvent>(TEvent eventData);

    /// <summary>订阅高频包裹事件。</summary>
    IDisposable SubscribePackage<TEvent>(Action<TEvent> handler);

    /// <summary>注册按发布顺序串行执行的异步高频包裹事件订阅者。</summary>
    IDisposable SubscribePackageAsync<TEvent>(Func<TEvent, Task> handler);

    /// <summary>取消普通应用事件订阅。</summary>
    void Unsubscribe<TEvent>(Action<TEvent> handler);

    /// <summary>取消高频包裹事件订阅。</summary>
    void UnsubscribePackage<TEvent>(Action<TEvent> handler);
}
