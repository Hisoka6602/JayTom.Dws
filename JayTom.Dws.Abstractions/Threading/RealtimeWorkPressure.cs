using System.Collections.Concurrent;

namespace JayTom.Dws.Abstractions.Threading;

/// <summary>让非实时读取在业务持久化积压时等待，不对分拣生产者施加背压。</summary>
public static class RealtimeWorkPressure
{
    /// <summary>待写业务项达到该数量时，历史读取先让出资源。</summary>
    private const long HistoryReadPauseThreshold = 128;
    /// <summary>保存持久化消费者的无锁队列长度读取器。</summary>
    private static readonly ConcurrentDictionary<long, Func<long>> Sources = new();
    /// <summary>分配独立注册标识，允许服务停止和重新启动。</summary>
    private static long _nextId;

    /// <summary>读取所有正在运行的业务写入队列长度，不包含等待主记录的孤立指令。</summary>
    public static long PendingPersistenceCount
    {
        get
        {
            long total = 0;
            foreach (var source in Sources.Values) total += Math.Max(0, source());
            return total;
        }
    }

    /// <summary>注册服务的常数时间队列读数；服务释放时须同时释放注册。</summary>
    public static IDisposable Register(Func<long> pendingCount)
    {
        ArgumentNullException.ThrowIfNull(pendingCount);
        var id = Interlocked.Increment(ref _nextId);
        Sources.TryAdd(id, pendingCount);
        return new Registration(id);
    }

    /// <summary>异步等待业务积压下降；等待期间不打开数据库连接，不占用 UI 或读取线程。</summary>
    public static async Task WaitForReadOpportunityAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (PendingPersistenceCount >= HistoryReadPauseThreshold)
            await Task.Delay(25, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>及时移除已经停止的持久化服务，不让其残留积压阻止后续查询。</summary>
    private sealed class Registration(long id) : IDisposable
    {
        /// <summary>释放注册；重复释放不会影响其他服务。</summary>
        public void Dispose() => Sources.TryRemove(id, out _);
    }
}
