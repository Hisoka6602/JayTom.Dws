using System;
using System.Threading;

namespace JayTom.Dws.Client.Service.Sorting
{
    /// <summary>维护单包裹 OCR 原始参数及可取消的共享调度器截止任务。</summary>
    internal sealed class PendingOcrSorting(SortingParam param) : IDisposable
    {
        /// <summary>原包裹分拣参数。</summary>
        public SortingParam Param { get; } = param;
        /// <summary>共享调度器中的截止任务。</summary>
        private IDisposable? _deadline;
        /// <summary>截止任务是否已经取消。</summary>
        private int _disposed;
        /// <summary>注册截止任务并处理取消与注册并发的情况。</summary>
        public void SetDeadline(IDisposable deadline)
        {
            Interlocked.Exchange(ref _deadline, deadline)?.Dispose();
            if (Volatile.Read(ref _disposed) != 0) Interlocked.Exchange(ref _deadline, null)?.Dispose();
        }
        /// <summary>原子取消截止任务，可重复调用。</summary>
        public void Dispose()
        {
            Interlocked.Exchange(ref _disposed, 1);
            Interlocked.Exchange(ref _deadline, null)?.Dispose();
        }
    }
}
