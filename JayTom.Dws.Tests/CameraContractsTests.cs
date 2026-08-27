using System.Buffers;
using JayTom.Dws.Camera.Contracts;

namespace JayTom.Dws.Tests;

/// <summary>验证跨平台相机契约的内存所有权和背压不变量。</summary>
public sealed class CameraContractsTests {
    /// <summary>帧租约释放后应归还所有者并阻止继续访问。</summary>
    [Fact]
    public void Image_frame_lease_returns_owned_memory_once() {
        var owner = new CountingMemoryOwner(16);
        var frame = new ImageFrameLease(
            owner,
            length: 8,
            width: 2,
            height: 2,
            stride: 4,
            FramePixelFormat.Gray8,
            DateTimeOffset.Now);

        Assert.Equal(8, frame.Data.Length);
        frame.Dispose();
        frame.Dispose();

        Assert.Equal(1, owner.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => frame.Data);
    }

    /// <summary>无效帧尺寸和无界通道容量必须在边界处失败。</summary>
    [Fact]
    public void Invalid_frame_and_channel_dimensions_are_rejected() {
        using var owner = new CountingMemoryOwner(8);

        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageFrameLease(
            owner,
            length: 0,
            width: 1,
            height: 1,
            stride: 1,
            FramePixelFormat.Gray8,
            DateTimeOffset.Now));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FrameChannelOptions.Create(0, FrameBackpressureMode.Wait));
    }

    private sealed class CountingMemoryOwner : IMemoryOwner<byte> {
        private byte[]? _buffer;

        public CountingMemoryOwner(int length) => _buffer = new byte[length];

        public int DisposeCount { get; private set; }
        public Memory<byte> Memory => _buffer ?? throw new ObjectDisposedException(
            nameof(CountingMemoryOwner));

        public void Dispose() {
            if (_buffer is null) {
                return;
            }

            _buffer = null;
            DisposeCount++;
        }
    }
}
