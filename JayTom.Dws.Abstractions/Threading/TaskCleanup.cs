namespace JayTom.Dws.Abstractions.Threading;

/// <summary>在同步释放入口中观察异步清理任务，避免阻塞线程或遗失异常。</summary>
public static class TaskCleanup
{
    /// <summary>观察清理任务；任务尚未完成时在默认调度器上完成观察。</summary>
    /// <param name="cleanupTask">异步清理任务。</param>
    /// <param name="onError">可选的清理错误接收器。</param>
    public static void Observe(Task cleanupTask, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(cleanupTask);
        if (cleanupTask.IsCompleted)
        {
            ObserveCompletion(cleanupTask, onError);
            return;
        }

        Task observation = cleanupTask.ContinueWith(
            completed => ObserveCompletion(completed, onError),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    /// <summary>读取任务异常并通知错误接收器。</summary>
    private static void ObserveCompletion(Task task, Action<Exception>? onError)
    {
        if (task.IsFaulted && task.Exception is not null)
        {
            try
            {
                onError?.Invoke(task.Exception.GetBaseException());
            }
            catch
            {
                // 清理错误接收器自身不得形成新的未观察异常。
            }
        }
    }
}
