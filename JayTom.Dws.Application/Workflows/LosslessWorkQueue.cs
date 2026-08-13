using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using JayTom.Dws.Abstractions.Workflows;

namespace JayTom.Dws.Application.Workflows;

/// <summary>为不可丢弃的业务数据提供非阻塞、多生产者工作队列。</summary>
public sealed class LosslessWorkQueue<T> : IWorkQueue<T>
{
    /// <summary>保存不可丢弃的工作项。</summary>
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    /// <summary>获取队列当前是否为空。</summary>
    public bool IsEmpty => !_channel.Reader.TryPeek(out _);

    /// <summary>异步写入工作项。</summary>
    public ValueTask EnqueueAsync(
        T item,
        CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(item, cancellationToken);

    /// <summary>异步读取下一个工作项。</summary>
    public ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAsync(cancellationToken);

    /// <summary>立即写入工作项，不等待消费者。</summary>
    public bool TryEnqueue(T item) => _channel.Writer.TryWrite(item);

    /// <summary>立即尝试读取一个工作项。</summary>
    public bool TryDequeue([MaybeNullWhen(false)] out T item) =>
        _channel.Reader.TryRead(out item);

    /// <summary>清除当前已经排队的全部工作项。</summary>
    public void Clear()
    {
        while (_channel.Reader.TryRead(out _))
        {
        }
    }
}
