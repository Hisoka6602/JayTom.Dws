namespace JayTom.Dws.Application.Workflows;

/// <summary>统一协调展示命令的忙碌状态、重复执行保护与取消请求。</summary>
public sealed class AsyncOperationController : IDisposable
{
    /// <summary>创建异步操作控制器。</summary>
    public AsyncOperationController()
    {
    }

    /// <summary>保护运行状态的同步根。</summary>
    private readonly object _syncRoot = new();

    /// <summary>当前操作的取消源。</summary>
    private CancellationTokenSource? _cancellation;

    /// <summary>指示控制器是否已释放。</summary>
    private bool _disposed;

    /// <summary>操作忙碌状态发生变化时触发。</summary>
    public event EventHandler? StateChanged;

    /// <summary>获取当前是否已有操作正在执行。</summary>
    public bool IsBusy
    {
        get
        {
            lock (_syncRoot)
            {
                return _cancellation is not null;
            }
        }
    }

    /// <summary>尝试执行操作，并在同一时刻只允许一个操作进入。</summary>
    /// <param name="operation">支持取消的异步操作。</param>
    /// <returns>操作成功进入执行队列时返回 <see langword="true"/>，忙碌时返回 <see langword="false"/>。</returns>
    public async Task<bool> TryRunAsync(Func<CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        CancellationTokenSource cancellation;
        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_cancellation is not null)
            {
                return false;
            }

            cancellation = new CancellationTokenSource();
            _cancellation = cancellation;
        }

        OnStateChanged();
        try
        {
            await operation(cancellation.Token);
            return true;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return true;
        }
        finally
        {
            lock (_syncRoot)
            {
                if (ReferenceEquals(_cancellation, cancellation))
                {
                    _cancellation = null;
                }
            }

            cancellation.Dispose();
            OnStateChanged();
        }
    }

    /// <summary>请求取消当前正在执行的操作。</summary>
    public void Cancel()
    {
        CancellationTokenSource? cancellation;
        lock (_syncRoot)
        {
            cancellation = _cancellation;
        }

        try
        {
            cancellation?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // 操作可能恰好在取消请求到达前完成，视为已取消完成。
        }
    }

    /// <summary>释放控制器并请求取消仍在执行的操作。</summary>
    public void Dispose()
    {
        CancellationTokenSource? cancellation;
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            cancellation = _cancellation;
        }

        try
        {
            cancellation?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // 并发完成路径已经释放取消源，无需再次处理。
        }
    }

    /// <summary>发布忙碌状态变化通知。</summary>
    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
