namespace JayTom.Dws.Camera.Concurrency;

/// <summary>为一次采集分配代号，拒绝停止后或上一轮 SDK 回调送来的工作项。</summary>
internal sealed class CameraCaptureSession {
    /// <summary>当前采集代号，停止和重新启动都会递增。</summary>
    private int _generation;
    /// <summary>标记当前是否允许处理采集结果。</summary>
    private int _accepting;

    /// <summary>开启新一轮采集并返回应由 SDK 回调捕获的代号。</summary>
    public int Start() {
        var generation = Interlocked.Increment(ref _generation);
        Volatile.Write(ref _accepting, 1);
        return generation;
    }

    /// <summary>使当前轮所有尚未开始处理的工作项失效。</summary>
    public void Stop() {
        Volatile.Write(ref _accepting, 0);
        Interlocked.Increment(ref _generation);
    }

    /// <summary>判断工作项是否属于仍在接收结果的当前采集。</summary>
    public bool IsCurrent(int generation) =>
        Volatile.Read(ref _accepting) != 0 && generation == Volatile.Read(ref _generation);
}
