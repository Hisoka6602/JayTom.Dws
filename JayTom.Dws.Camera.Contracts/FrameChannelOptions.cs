// DWS-COHESIVE-CONTRACTS: 通道容量和背压枚举必须原子配置。
namespace JayTom.Dws.Camera.Contracts;

/// <summary>定义相机帧通道容量和背压行为。</summary>
public sealed record FrameChannelOptions(int Capacity, FrameBackpressureMode BackpressureMode) {
    /// <summary>创建经过校验的通道选项。</summary>
    public static FrameChannelOptions Create(
        int capacity,
        FrameBackpressureMode backpressureMode) {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        return new FrameChannelOptions(capacity, backpressureMode);
    }
}

/// <summary>定义帧通道满载时的处理方式。</summary>
public enum FrameBackpressureMode {
    /// <summary>等待消费者，保证不丢帧。</summary>
    Wait,
    /// <summary>丢弃最旧帧，保持最新画面。</summary>
    DropOldest,
    /// <summary>拒绝新帧并记录指标。</summary>
    RejectNewest
}
