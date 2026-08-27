namespace JayTom.Dws.Application.Messaging;

using JayTom.Dws.Abstractions.Observability;
using System.Diagnostics;

/// <summary>
/// 为单个异步订阅者提供有界、按序且故障隔离的事件调度。
/// </summary>
/// <typeparam name="TEvent">事件载荷类型。</typeparam>
public sealed class SequentialAsyncEventHandler<TEvent> : IDisposable
{
    /// <summary>实际异步事件处理器。</summary>
    private readonly Func<TEvent, Task> _handler;

    /// <summary>处理器异常观察回调。</summary>
    private readonly Action<Exception>? _onError;

    /// <summary>单个订阅者允许积压的最大事件数。</summary>
    private readonly int _capacity;

    /// <summary>保护队列与释放状态的同步门。</summary>
    private readonly object _lifecycleGate = new();

    /// <summary>保存等待处理的事件。</summary>
    private readonly Queue<TEvent> _queue = new();

    /// <summary>指示是否已经有排空任务在运行。</summary>
    private int _isDraining;

    /// <summary>拥有当前排空任务；包装层保证任何意外异常都会进入统一观察回调。</summary>
    private Task _drainTask = Task.CompletedTask;

    /// <summary>指示订阅是否已经释放。</summary>
    private bool _isDisposed;

    /// <summary>创建有序异步事件处理器。</summary>
    /// <param name="handler">实际异步处理回调。</param>
    /// <param name="capacity">允许等待的最大事件数。</param>
    /// <param name="onError">可选的异常观察回调。</param>
    public SequentialAsyncEventHandler(
        Func<TEvent, Task> handler,
        int capacity = 256,
        Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _handler = handler;
        _capacity = capacity;
        _onError = onError;
    }

    /// <summary>尝试将事件加入当前订阅者的串行任务链。</summary>
    /// <param name="eventData">待处理事件。</param>
    /// <returns>事件成功进入队列时返回真；已释放或发生背压时返回假。</returns>
    public bool TryEnqueue(TEvent eventData)
    {
        lock (_lifecycleGate)
        {
            if (_isDisposed || _queue.Count >= _capacity)
            {
                return false;
            }

            _queue.Enqueue(eventData);
        }

        StartDrainIfRequired();
        return true;
    }

    /// <summary>在尚未运行排空任务时启动唯一的串行消费者。</summary>
    private void StartDrainIfRequired()
    {
        if (Interlocked.CompareExchange(ref _isDraining, 1, 0) == 0)
        {
            _drainTask = DrainSafelyAsync();
        }
    }

    /// <summary>执行队列排空并观察排空器自身的意外故障。</summary>
    private async Task DrainSafelyAsync()
    {
        try
        {
            await DrainAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Interlocked.Exchange(ref _isDraining, 0);
            ObserveError(exception);
        }
    }

    /// <summary>按入队顺序逐个执行处理器，并隔离单次处理异常。</summary>
    private async Task DrainAsync()
    {
        do
        {
            while (TryTake(out var eventData))
            {
                try
                {
                    using CorrelationScope correlation = CorrelationContext.Begin();
                    using Activity? activity = DwsDiagnostics.StartActivity(
                        $"event.consume.{typeof(TEvent).Name}");
                    long started = Stopwatch.GetTimestamp();
                    await _handler(eventData).ConfigureAwait(false);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    DwsDiagnostics.RecordOperation(
                        "event.consume",
                        true,
                        Stopwatch.GetElapsedTime(started));
                }
                catch (Exception exception)
                {
                    DwsDiagnostics.RecordOperation("event.consume", false, TimeSpan.Zero);
                    ObserveError(exception);
                }
            }

            Interlocked.Exchange(ref _isDraining, 0);
        }
        while (HasPendingEvents() &&
               Interlocked.CompareExchange(ref _isDraining, 1, 0) == 0);
    }

    /// <summary>在生命周期锁内取出一个尚未开始处理的事件。</summary>
    /// <param name="eventData">成功取出的事件。</param>
    /// <returns>存在可处理事件时返回真。</returns>
    private bool TryTake(out TEvent eventData)
    {
        lock (_lifecycleGate)
        {
            if (!_isDisposed && _queue.TryDequeue(out eventData!))
            {
                return true;
            }
        }

        eventData = default!;
        return false;
    }

    /// <summary>判断释放前是否仍有待处理事件。</summary>
    private bool HasPendingEvents()
    {
        lock (_lifecycleGate)
        {
            return !_isDisposed && _queue.Count > 0;
        }
    }

    /// <summary>安全通知异常观察者，避免观察逻辑破坏事件排空循环。</summary>
    /// <param name="exception">处理器抛出的异常。</param>
    private void ObserveError(Exception exception)
    {
        try
        {
            _onError?.Invoke(exception);
        }
        catch
        {
            // 异常观察器不得中断后续事件处理。
        }
    }

    /// <summary>停止接收新事件，并清除尚未开始处理的积压事件。</summary>
    public void Dispose()
    {
        lock (_lifecycleGate)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _queue.Clear();
        }
    }
}
