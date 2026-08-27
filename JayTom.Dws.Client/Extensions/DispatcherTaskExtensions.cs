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
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        _ = ObserveAsync(task, operationName);
    }

    /// <summary>等待后台任务并隔离日志记录器自身的异常，保证观察任务永不故障。</summary>
    private static async Task ObserveAsync(Task task, string operationName)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // 取消是受控生命周期结果，无需按故障记录。
        }
        catch (Exception exception)
        {
            try
            {
                NLog.LogManager.GetCurrentClassLogger().Error(
                    exception,
                    $"后台任务执行失败:{operationName}");
            }
            catch
            {
                // 日志基础设施不得让异常重新变为未观察任务异常。
            }
        }
    }
}
