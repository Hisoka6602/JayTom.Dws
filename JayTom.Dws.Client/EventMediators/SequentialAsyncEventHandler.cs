using System;
using System.Threading.Tasks;

namespace JayTom.Dws.Client.EventMediators {

    /// <summary>
    /// 为单个异步订阅者按发布顺序串行执行事件，并统一记录处理异常。
    /// </summary>
    internal sealed class SequentialAsyncEventHandler<TEventType> {
        /// <summary>
        /// 实际异步事件处理器。
        /// </summary>
        private readonly Func<TEventType, Task> _handler;
        /// <summary>
        /// 保护任务尾指针的同步对象。
        /// </summary>
        private readonly object _syncRoot = new();
        /// <summary>
        /// 当前订阅者最后一个已排队任务。
        /// </summary>
        private Task _tail = Task.CompletedTask;

        /// <summary>
        /// 创建有序异步事件处理器。
        /// </summary>
        public SequentialAsyncEventHandler(Func<TEventType, Task> handler) {
            ArgumentNullException.ThrowIfNull(handler);
            _handler = handler;
        }

        /// <summary>
        /// 将事件追加到当前订阅者的串行任务链。
        /// </summary>
        public void Enqueue(TEventType eventData) {
            lock (_syncRoot) {
                _tail = InvokeAfterAsync(_tail, eventData);
            }
        }

        /// <summary>
        /// 等待前一事件完成后执行当前事件。
        /// </summary>
        private async Task InvokeAfterAsync(Task previous, TEventType eventData) {
            try {
                await previous.ConfigureAwait(false);
                await _handler(eventData).ConfigureAwait(false);
            }
            catch (Exception exception) {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(exception, "异步事件订阅处理失败");
            }
        }
    }
}
