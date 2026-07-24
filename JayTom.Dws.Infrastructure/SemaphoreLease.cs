namespace JayTom.Dws.Infrastructure {

    /// <summary>
    /// 表示一次可自动释放的信号量占用，避免取消等待时错误增加信号量计数。
    /// </summary>
    internal sealed class SemaphoreLease : IDisposable {

        /// <summary>
        /// 保存当前占用的信号量；释放后设置为空以保证幂等。
        /// </summary>
        private SemaphoreSlim? _semaphore;

        /// <summary>
        /// 初始化信号量占用实例。
        /// </summary>
        /// <param name="semaphore">已经成功进入的信号量。</param>
        private SemaphoreLease(SemaphoreSlim semaphore) {
            _semaphore = semaphore;
        }

        /// <summary>
        /// 异步等待并取得信号量占用。
        /// </summary>
        /// <param name="semaphore">需要进入的信号量。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>成功取得的信号量占用。</returns>
        public static async ValueTask<SemaphoreLease> EnterAsync(
            SemaphoreSlim semaphore,
            CancellationToken token) {
            ArgumentNullException.ThrowIfNull(semaphore);
            await semaphore.WaitAsync(token).ConfigureAwait(false);
            return new SemaphoreLease(semaphore);
        }

        /// <summary>
        /// 释放当前持有的信号量。
        /// </summary>
        public void Dispose() {
            Interlocked.Exchange(ref _semaphore, null)?.Release();
        }
    }
}
