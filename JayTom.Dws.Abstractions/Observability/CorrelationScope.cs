namespace JayTom.Dws.Abstractions.Observability;

/// <summary>在释放时恢复外层关联标识。</summary>
public sealed class CorrelationScope : IDisposable
{
    /// <summary>外层关联标识。</summary>
    private readonly string _previous;
    /// <summary>防止重复恢复。</summary>
    private int _disposed;

    /// <summary>创建关联作用域。</summary>
    internal CorrelationScope(string previous) => _previous = previous;

    /// <summary>恢复外层关联标识。</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            CorrelationContext.Restore(_previous);
        }
    }
}
