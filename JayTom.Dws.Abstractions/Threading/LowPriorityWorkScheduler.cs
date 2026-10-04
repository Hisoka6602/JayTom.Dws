using System.Collections.Concurrent;

namespace JayTom.Dws.Abstractions.Threading;

/// <summary>把历史读取隔离到独立线程，通过业务积压等待和请求上限让实时工作优先。</summary>
public sealed class LowPriorityWorkScheduler : TaskScheduler
{
    /// <summary>按进程复用一个历史读取通道，不占用分拣和接口工作的线程池。</summary>
    private static readonly Lazy<LowPriorityWorkScheduler> SharedInstance = new(() => new("DwsHistoryReads", 8));
    /// <summary>保存已获准工作和它们的异步续体。</summary>
    private readonly BlockingCollection<Task> _tasks = new();
    /// <summary>限制每个通道同时执行一个完整操作。</summary>
    private readonly SemaphoreSlim _executionGate = new(1, 1);
    /// <summary>限制在途请求，防止慢查询时无限积压。</summary>
    private readonly int _capacity;
    /// <summary>已经接收但尚未完成的请求数。</summary>
    private int _pendingCount;

    /// <summary>获取进程共享的低优先级历史读取调度器。</summary>
    public static LowPriorityWorkScheduler Shared => SharedInstance.Value;
    /// <summary>获取已经接收的请求数量，包含正在执行的请求。</summary>
    public int PendingCount => Volatile.Read(ref _pendingCount);

    /// <summary>建立随进程退出的后台线程；线程只处理非实时工作。</summary>
    public LowPriorityWorkScheduler(string name, int capacity = 8)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
        var thread = new Thread(Process)
        {
            Name = name,
            IsBackground = true,
            // CLR/JIT 等共享资源不能由被饿死的线程持有；通过积压准入和主动等待实现业务低优先级。
            Priority = ThreadPriority.Normal
        };
        thread.Start();
    }

    /// <summary>在独立线程执行完整异步操作，排队和执行错误均通过返回任务交给调用者。</summary>
    public async Task<T> RunAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.Increment(ref _pendingCount) > _capacity)
        {
            Interlocked.Decrement(ref _pendingCount);
            throw new InvalidOperationException("历史数据查询繁忙，请稍后重试。");
        }
        var entered = false;
        var admissionState = 1;
        using var cancellation = cancellationToken.Register(() =>
        {
            // 被替代的排队查询立即让出名额，不依赖线程池何时处理其取消续体。
            if (Interlocked.CompareExchange(ref admissionState, 0, 1) == 1)
                Interlocked.Decrement(ref _pendingCount);
        });
        try
        {
            await _executionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            entered = true;
            Interlocked.CompareExchange(ref admissionState, 2, 1);
            cancellationToken.ThrowIfCancellationRequested();
            await RealtimeWorkPressure.WaitForReadOpportunityAsync(cancellationToken).ConfigureAwait(false);
            // 查询之间主动让出 CPU；分拣入队、接口及 PLC 回复不经过此等待。
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            return await Task.Factory.StartNew(operation, cancellationToken,
                TaskCreationOptions.DenyChildAttach, this).Unwrap().ConfigureAwait(false);
        }
        finally
        {
            if (entered) _executionGate.Release();
            if (Interlocked.Exchange(ref admissionState, 0) != 0) Interlocked.Decrement(ref _pendingCount);
        }
    }

    /// <summary>在同一个低优先级通道执行同步映射等有界工作。</summary>
    public Task<T> RunAsync<T>(Func<T> operation, CancellationToken cancellationToken = default) =>
        RunAsync(() => Task.FromResult(operation()), cancellationToken);

    /// <summary>为调试器提供排队任务快照。</summary>
    protected override IEnumerable<Task> GetScheduledTasks() => _tasks.ToArray();
    /// <summary>所有任务及续体均进入后台线程，禁止在 UI 或设备线程内联。</summary>
    protected override void QueueTask(Task task) => _tasks.Add(task);
    /// <summary>即使调度器空闲也不允许调用方执行历史读取。</summary>
    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
    /// <summary>持续执行任务；任务异常由 TPL 保存，不会终止后台线程。</summary>
    private void Process()
    {
        foreach (var task in _tasks.GetConsumingEnumerable()) TryExecuteTask(task);
    }
}
