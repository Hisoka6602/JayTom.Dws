using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Abstractions.Workflows;

/// <summary>
/// 后台工作流使用的有界且支持取消的队列。
/// </summary>
public interface IWorkQueue<T> {
    /// <summary>获取队列当前是否为空。</summary>
    bool IsEmpty { get; }

    /// <summary>异步写入一个工作项。</summary>
    ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default);

    /// <summary>异步读取下一个工作项。</summary>
    ValueTask<T> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>在队列未满时立即写入工作项。</summary>
    bool TryEnqueue(T item);

    /// <summary>立即尝试读取工作项。</summary>
    bool TryDequeue([MaybeNullWhen(false)] out T item);

    /// <summary>清空当前已经排队的工作项。</summary>
    void Clear();
}
