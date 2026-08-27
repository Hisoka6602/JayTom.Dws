namespace JayTom.Dws.Application.UseCases;

/// <summary>应用读用例处理器。</summary>
public interface IApplicationQueryHandler<in TQuery, TResult>
    where TQuery : IApplicationQuery<TResult>
{
    /// <summary>执行只读用例。</summary>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
