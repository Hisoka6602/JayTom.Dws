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

    /// <summary>通过原子增减保存队列长度，避免在设备回调上遍历或锁定队列。</summary>
    private long _count;

    /// <summary>获取尚未被消费者取走的工作数，包含正在完成入队的工作。</summary>
    public long Count => Volatile.Read(ref _count);

    /// <summary>获取队列当前是否为空。</summary>
    public bool IsEmpty => !_channel.Reader.TryPeek(out _);

    /// <summary>异步写入工作项。</summary>
    public ValueTask EnqueueAsync(
        T item,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return ValueTask.FromCanceled(cancellationToken);
        return TryEnqueue(item) ? ValueTask.CompletedTask : ValueTask.FromException(new ChannelClosedException());
    }

    /// <summary>异步读取下一个工作项。</summary>
    public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var item = await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        Interlocked.Decrement(ref _count);
        return item;
    }

    /// <summary>立即写入工作项，不等待消费者。</summary>
    public bool TryEnqueue(T item)
    {
        // 先增加计数，再让工作对消费者可见，避免并发读取得到负数。
        Interlocked.Increment(ref _count);
        if (_channel.Writer.TryWrite(item)) return true;
        Interlocked.Decrement(ref _count);
        return false;
    }

    /// <summary>立即尝试读取一个工作项。</summary>
    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        if (!_channel.Reader.TryRead(out item)) return false;
        Interlocked.Decrement(ref _count);
        return true;
    }

    /// <summary>清除当前已经排队的全部工作项。</summary>
    public void Clear()
    {
        while (TryDequeue(out _))
        {
        }
    }
}
