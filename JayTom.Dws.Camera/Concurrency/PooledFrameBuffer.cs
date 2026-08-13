using System.Buffers;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Camera.Concurrency;

/// <summary>从相机 SDK 非托管帧指针复制数据，并通过共享数组池降低大图帧的 GC 压力。</summary>
internal sealed class PooledFrameBuffer : IDisposable {
    /// <summary>当前租用的托管缓冲区。</summary>
    private byte[]? _buffer;

    /// <summary>初始化一个已经从共享数组池租用的帧缓冲区。</summary>
    private PooledFrameBuffer(byte[] buffer, int length) {
        _buffer = buffer;
        Length = length;
    }

    /// <summary>获取包含有效帧数据的托管缓冲区。</summary>
    public byte[] Buffer => _buffer ?? throw new ObjectDisposedException(nameof(PooledFrameBuffer));

    /// <summary>获取缓冲区内有效帧数据的字节数。</summary>
    public int Length { get; }

    /// <summary>立即复制 SDK 帧指针，确保原生回调或下一次拉帧后仍可安全异步处理。</summary>
    public static PooledFrameBuffer CopyFrom(IntPtr source, int length) {
        if (source == IntPtr.Zero) {
            throw new ArgumentException("相机帧指针不能为空。", nameof(source));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try {
            Marshal.Copy(source, buffer, 0, length);
            return new PooledFrameBuffer(buffer, length);
        }
        catch {
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

    /// <summary>从托管 SDK 帧数组复制有效数据，避免厂商回调返回后复用其缓冲区。</summary>
    public static PooledFrameBuffer CopyFrom(byte[] source, int length) {
        ArgumentNullException.ThrowIfNull(source);
        if (length <= 0 || length > source.Length) {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try {
            source.AsSpan(0, length).CopyTo(buffer);
            return new PooledFrameBuffer(buffer, length);
        }
        catch {
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

    /// <summary>将租用缓冲区归还共享数组池。</summary>
    public void Dispose() {
        var buffer = Interlocked.Exchange(ref _buffer, null);
        if (buffer is not null) {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
