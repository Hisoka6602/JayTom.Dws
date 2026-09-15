namespace JayTom.Dws.Camera.Concurrency;

/// <summary>为没有可靠触发结束回调的相机合并连续触发帧，只在一轮全部无码时发布一个 NoRead。</summary>
internal sealed class TriggerBurstNoReadGate : IDisposable {
    private readonly object _syncRoot = new();
    private readonly TimeSpan _quietPeriod;
    private readonly Action<int, BarcodeReadEventArgs> _publisher;
    private readonly Action<Exception>? _exceptionHandler;
    private readonly Timer _timer;
    private BarcodeReadEventArgs? _pendingNoRead;
    private bool _hasFrame;
    private bool _hasBarcode;
    private bool _isDisposed;
    private int _generation;

    /// <summary>创建按连续回调静默时间收束触发帧组的 NoRead 门控器。</summary>
    /// <param name="quietPeriod">最后一帧到触发帧组完成之间的静默时间。</param>
    /// <param name="publisher">触发帧组全部无码时调用的发布函数。</param>
    /// <param name="exceptionHandler">发布函数抛出异常时调用的错误处理函数。</param>
    public TriggerBurstNoReadGate(
        TimeSpan quietPeriod,
        Action<int, BarcodeReadEventArgs> publisher,
        Action<Exception>? exceptionHandler = null) {
        if (quietPeriod <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(quietPeriod));
        }
        ArgumentNullException.ThrowIfNull(publisher);
        _quietPeriod = quietPeriod;
        _publisher = publisher;
        _exceptionHandler = exceptionHandler;
        _timer = new Timer(
            static state => ((TriggerBurstNoReadGate)state!).Complete(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
    }

    /// <summary>记录一帧原始 SDK 读码结果，并接管可选的 NoRead 候选图像。</summary>
    /// <param name="generation">产生该帧的采集会话代号。</param>
    /// <param name="hasBarcode">SDK 在过滤前是否识别到至少一个条码。</param>
    /// <param name="candidate">当前帧的 NoRead 候选；传入后由门控器负责释放或发布。</param>
    public void Observe(int generation, bool hasBarcode, BarcodeReadEventArgs? candidate) {
        BarcodeReadEventArgs? staleCandidate = null;
        BarcodeReadEventArgs? rejectedCandidate = null;
        lock (_syncRoot) {
            if (_isDisposed) {
                rejectedCandidate = candidate;
            }
            else {
                if (_hasFrame && _generation != generation) {
                    staleCandidate = _pendingNoRead;
                    ResetCycle();
                }

                _generation = generation;
                _hasFrame = true;
                if (hasBarcode) {
                    _hasBarcode = true;
                    staleCandidate ??= _pendingNoRead;
                    _pendingNoRead = null;
                    rejectedCandidate = candidate;
                }
                else if (_hasBarcode || _pendingNoRead is not null) {
                    rejectedCandidate = candidate;
                }
                else {
                    _pendingNoRead = candidate;
                }

                _timer.Change(_quietPeriod, Timeout.InfiniteTimeSpan);
            }
        }

        Release(staleCandidate);
        if (!ReferenceEquals(staleCandidate, rejectedCandidate)) {
            Release(rejectedCandidate);
        }
    }

    /// <summary>立即完成当前触发帧组；主要供明确结束回调和确定性测试使用。</summary>
    public void Complete() {
        BarcodeReadEventArgs? candidate;
        int generation;
        lock (_syncRoot) {
            if (_isDisposed || !_hasFrame) {
                return;
            }

            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            generation = _generation;
            candidate = _hasBarcode ? null : _pendingNoRead;
            if (_hasBarcode) {
                Release(_pendingNoRead);
            }
            ResetCycle();
        }

        if (candidate is null) {
            return;
        }
        try {
            _publisher(generation, candidate);
        }
        catch (Exception exception) {
            _exceptionHandler?.Invoke(exception);
        }
    }

    /// <summary>停止计时并释放尚未完成触发帧组持有的图像。</summary>
    public void Dispose() {
        BarcodeReadEventArgs? candidate;
        lock (_syncRoot) {
            if (_isDisposed) {
                return;
            }
            _isDisposed = true;
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            candidate = _pendingNoRead;
            ResetCycle();
        }
        _timer.Dispose();
        Release(candidate);
    }

    /// <summary>清空一轮触发帧组的聚合状态。</summary>
    private void ResetCycle() {
        _pendingNoRead = null;
        _hasFrame = false;
        _hasBarcode = false;
        _generation = 0;
    }

    /// <summary>释放未交给业务订阅者的事件图像。</summary>
    private static void Release(BarcodeReadEventArgs? args) {
        args?.Image?.Dispose();
        if (args is not null && !ReferenceEquals(args.Image, args.ThumbImage)) {
            args.ThumbImage?.Dispose();
        }
    }
}
