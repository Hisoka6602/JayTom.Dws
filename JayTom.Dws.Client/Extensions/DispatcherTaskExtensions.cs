using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace System;

/// <summary>
/// 提供能够展开并等待异步委托的调度器扩展。
/// </summary>
public static class DispatcherTaskExtensions
{
    /// <summary>
    /// 在调度器线程执行异步委托，并等待委托内部任务真正完成。
    /// </summary>
    public static async Task InvokeAsyncUnwrapped(
        this Dispatcher dispatcher,
        Func<Task> callback,
        DispatcherPriority priority = DispatcherPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(callback);

        var callbackTask = await dispatcher.InvokeAsync(callback, priority, cancellationToken);
        await callbackTask;
    }

    /// <summary>
    /// 明确启动无需阻塞调用方的任务，并记录后台执行异常。
    /// </summary>
    public static void Forget(this Task task, string operationName)
    {
        ArgumentNullException.ThrowIfNull(task);
        _ = task.ContinueWith(
            completed => NLog.LogManager.GetCurrentClassLogger().Error(
                completed.Exception,
                $"后台任务执行失败:{operationName}"),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
