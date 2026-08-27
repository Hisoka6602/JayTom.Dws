using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.UseCases;

/// <summary>应用写用例处理器。</summary>
public interface IApplicationCommandHandler<in TCommand, TResult>
    where TCommand : IApplicationCommand<TResult>
{
    /// <summary>执行写用例。</summary>
    Task<OperationResult<TResult>> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default);
}
