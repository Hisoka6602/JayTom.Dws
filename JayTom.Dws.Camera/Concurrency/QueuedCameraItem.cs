namespace JayTom.Dws.Camera.Concurrency;

/// <summary>保存相机工作项及其单调时钟入队时间，用于定位 SDK 回调后的排队延迟。</summary>
/// <typeparam name="T">相机工作项类型。</typeparam>
internal readonly record struct QueuedCameraItem<T>(
    T Item,
    long EnqueuedAtTimestamp);
