namespace JayTom.Dws.Application.Workflows;

/// <summary>保存可原子取消、仅执行一次的单调时钟任务。</summary>
internal sealed class MonotonicScheduledItem : IDisposable
{
    /// <summary>非空表示任务仍可执行。</summary>
    private Action? _callback;

    /// <summary>创建一次性截止任务。</summary>
    public MonotonicScheduledItem(Action callback) => _callback = callback;

    /// <summary>获取任务是否已经取消或执行。</summary>
    public bool IsCancelled => Volatile.Read(ref _callback) is null;

    /// <summary>原子取得并执行一次回调。</summary>
    public void TryInvoke() => Interlocked.Exchange(ref _callback, null)?.Invoke();

    /// <summary>原子取消尚未执行的任务。</summary>
    public void Dispose() => Interlocked.Exchange(ref _callback, null);
}
