using System.Threading.Channels;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Abstractions.Workflows;

namespace JayTom.Dws.Application.Workflows;

/// <summary>
/// 基于通道实现并带有显式背压的有界工作队列。
/// </summary>
public sealed class BoundedWorkQueue<T> : IWorkQueue<T> {
    /// <summary>承载工作项并提供背压的内部通道。</summary>
    private readonly Channel<T> _channel;

    /// <summary>获取队列当前是否为空。</summary>
    public bool IsEmpty => !_channel.Reader.TryPeek(out _);

    /// <summary>使用指定容量创建工作队列。</summary>
    public BoundedWorkQueue(int capacity) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    /// <summary>异步写入一个工作项。</summary>
    public ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(item, cancellationToken);

    /// <summary>异步读取下一个工作项。</summary>
    public ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAsync(cancellationToken);

    /// <summary>在队列未满时立即写入工作项。</summary>
    public bool TryEnqueue(T item) => _channel.Writer.TryWrite(item);

    /// <summary>立即尝试读取工作项。</summary>
    public bool TryDequeue([MaybeNullWhen(false)] out T item) =>
        _channel.Reader.TryRead(out item);

    /// <summary>清空当前已经排队的工作项。</summary>
    public void Clear() {
        while (_channel.Reader.TryRead(out _)) {
        }
    }
}
