using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Application.Messaging;
using Prism.Events;
using JayTom.Dws.Client.Observability;

namespace JayTom.Dws.Client.EventMediators;

/// <summary>通过 Prism 在进程内发布和订阅应用事件。</summary>
public sealed class EventAggregator : IEventBus
{
    /// <summary>普通应用事件聚合器。</summary>
    private readonly IEventAggregator _eventAggregator = new Prism.Events.EventAggregator();

    /// <summary>高频包裹事件聚合器。</summary>
    private readonly IEventAggregator _packageEventAggregator = new Prism.Events.EventAggregator();

    /// <summary>让异步订阅包装器与其真实订阅目标具有相同生命周期。</summary>
    private readonly ConditionalWeakTable<object, List<object>> _asyncSubscriptions = new();

    /// <summary>保存没有实例目标的静态异步订阅包装器。</summary>
    private readonly List<object> _staticAsyncSubscriptions = [];

    /// <summary>发布普通应用事件。</summary>
    public void Publish<TEvent>(TEvent eventData) =>
        _eventAggregator.GetEvent<PubSubEvent<TEvent>>().Publish(eventData);

    /// <summary>订阅普通应用事件。</summary>
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        var applicationEvent = _eventAggregator.GetEvent<PubSubEvent<TEvent>>();
        var token = applicationEvent.Subscribe(
            handler,
            ThreadOption.PublisherThread,
            false);
        return new EventSubscription(() => applicationEvent.Unsubscribe(token));
    }

    /// <summary>订阅异步事件处理器，并按发布顺序观察处理异常。</summary>
    private IDisposable SubscribeSequential<TEvent>(
        IEventAggregator aggregator,
        Func<TEvent, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var subscription = new SequentialAsyncEventHandler<TEvent>(
            handler,
            onError: exception => NLog.LogManager.GetCurrentClassLogger()
                .ErrorSanitized(exception, "event.consume"));
        Action releaseRetention;
        if (handler.Target is { } target)
        {
            var subscriptions = _asyncSubscriptions.GetValue(target, static _ => []);
            lock (subscriptions)
            {
                subscriptions.Add(subscription);
            }
            releaseRetention = () =>
            {
                lock (subscriptions)
                {
                    subscriptions.Remove(subscription);
                }
            };
        }
        else
        {
            lock (_staticAsyncSubscriptions)
            {
                _staticAsyncSubscriptions.Add(subscription);
            }
            releaseRetention = () =>
            {
                lock (_staticAsyncSubscriptions)
                {
                    _staticAsyncSubscriptions.Remove(subscription);
                }
            };
        }

        var applicationEvent = aggregator.GetEvent<PubSubEvent<TEvent>>();
        var token = applicationEvent.Subscribe(
            eventData =>
            {
                if (!subscription.TryEnqueue(eventData))
                {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Error("异步事件订阅队列已满或已释放，拒绝最新事件");
                }
            },
            ThreadOption.PublisherThread,
            false);
        return new EventSubscription(() =>
        {
            applicationEvent.Unsubscribe(token);
            subscription.Dispose();
            releaseRetention();
        });
    }

    /// <summary>通过应用边界注册有界且有序的异步事件订阅者。</summary>
    public IDisposable SubscribeAsync<TEvent>(Func<TEvent, Task> handler) =>
        SubscribeSequential(_eventAggregator, handler);

    /// <summary>发布高频包裹事件。</summary>
    public void PublishPackage<TEvent>(TEvent eventData) =>
        _packageEventAggregator.GetEvent<PubSubEvent<TEvent>>().Publish(eventData);

    /// <summary>订阅高频包裹事件。</summary>
    public IDisposable SubscribePackage<TEvent>(Action<TEvent> handler)
    {
        var packageEvent = _packageEventAggregator.GetEvent<PubSubEvent<TEvent>>();
        var token = packageEvent.Subscribe(
            handler,
            ThreadOption.PublisherThread,
            false);
        return new EventSubscription(() => packageEvent.Unsubscribe(token));
    }

    /// <summary>通过应用边界注册有界且有序的异步高频包裹事件订阅者。</summary>
    public IDisposable SubscribePackageAsync<TEvent>(Func<TEvent, Task> handler) =>
        SubscribeSequential(_packageEventAggregator, handler);

    /// <summary>取消普通应用事件订阅。</summary>
    public void Unsubscribe<TEvent>(Action<TEvent> handler) =>
        _eventAggregator.GetEvent<PubSubEvent<TEvent>>().Unsubscribe(handler);

    /// <summary>使用订阅令牌取消普通应用事件订阅。</summary>
    public void Unsubscribe<TEvent>(SubscriptionToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        _eventAggregator.GetEvent<PubSubEvent<TEvent>>().Unsubscribe(token);
    }

    /// <summary>取消高频包裹事件订阅。</summary>
    public void UnsubscribePackage<TEvent>(Action<TEvent> handler) =>
        _packageEventAggregator.GetEvent<PubSubEvent<TEvent>>().Unsubscribe(handler);

}
