using System;
using System.Collections.Concurrent;
using System.Threading;
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
        /// <summary>单个订阅者允许积压的最大事件数。</summary>
        private const int Capacity = 256;

        /// <summary>保存等待处理的有界事件队列。</summary>
        private readonly ConcurrentQueue<TEventType> _queue = new();

        /// <summary>当前队列中的事件数量。</summary>
        private int _queuedCount;

        /// <summary>指示是否已经有排空任务在运行。</summary>
        private int _isDraining;

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
            var queuedCount = Interlocked.Increment(ref _queuedCount);
            if (queuedCount > Capacity) {
                Interlocked.Decrement(ref _queuedCount);
                NLog.LogManager.GetCurrentClassLogger()
                    .Error("异步事件订阅队列已满，拒绝最新事件；容量:{Capacity}", Capacity);
                return;
            }

            _queue.Enqueue(eventData);
            StartDrainIfRequired();
        }

        /// <summary>在尚未运行排空任务时启动唯一的串行消费者。</summary>
        private void StartDrainIfRequired() {
            if (Interlocked.CompareExchange(ref _isDraining, 1, 0) == 0) {
                _ = DrainAsync();
            }
        }

        /// <summary>按入队顺序逐个执行处理器，并确保处理异常不会中断后续事件。</summary>
        private async Task DrainAsync() {
            do {
                while (_queue.TryDequeue(out var eventData)) {
                    Interlocked.Decrement(ref _queuedCount);
                    try {
                        await _handler(eventData).ConfigureAwait(false);
                    }
                    catch (Exception exception) {
                        NLog.LogManager.GetCurrentClassLogger()
                            .Error(exception, "异步事件订阅处理失败");
                    }
                }

                Interlocked.Exchange(ref _isDraining, 0);
            }
            while (!_queue.IsEmpty &&
                   Interlocked.CompareExchange(ref _isDraining, 1, 0) == 0);
        }
    }
}
