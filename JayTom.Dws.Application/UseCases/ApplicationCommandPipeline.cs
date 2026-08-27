using System.Diagnostics;
using System.Text.Json;
using JayTom.Dws.Abstractions.Persistence;
using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.UseCases;

/// <summary>
/// 统一执行输入校验、幂等、事务提交、关联日志和用例指标。
/// </summary>
public sealed class ApplicationCommandPipeline<TCommand, TResult>
    where TCommand : IApplicationCommand<TResult> {
    private readonly IApplicationCommandHandler<TCommand, TResult> _handler;
    private readonly IReadOnlyList<IApplicationRequestValidator<TCommand>> _validators;
    private readonly IUnitOfWork? _unitOfWork;
    private readonly IApplicationIdempotencyStore? _idempotencyStore;
    private readonly IApplicationUseCaseTelemetry _telemetry;

    /// <summary>创建应用命令管道。</summary>
    public ApplicationCommandPipeline(
        IApplicationCommandHandler<TCommand, TResult> handler,
        IEnumerable<IApplicationRequestValidator<TCommand>> validators,
        IApplicationUseCaseTelemetry? telemetry = null,
        IUnitOfWork? unitOfWork = null,
        IApplicationIdempotencyStore? idempotencyStore = null) {
        _handler = handler;
        _validators = validators.ToArray();
        _telemetry = telemetry ?? NullApplicationUseCaseTelemetry.Instance;
        _unitOfWork = unitOfWork;
        _idempotencyStore = idempotencyStore;
    }

    /// <summary>通过完整横切管道执行命令。</summary>
    public async Task<OperationResult<TResult>> ExecuteAsync(
        TCommand command,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(command);
        var name = typeof(TCommand).FullName ?? typeof(TCommand).Name;
        var idempotencyKey = (command as IIdempotentApplicationCommand<TResult>)?
            .IdempotencyKey;
        using var scope = _telemetry.Begin(name, idempotencyKey);
        var started = Stopwatch.GetTimestamp();

        var validationError = _validators
            .SelectMany(validator => validator.Validate(command))
            .FirstOrDefault(error => error != Error.None);
        if (validationError is not null && validationError != Error.None) {
            var failure = OperationResult<TResult>.Failure(validationError);
            Record(failure, name, started);
            return failure;
        }

        if (!string.IsNullOrWhiteSpace(idempotencyKey) && _idempotencyStore is not null) {
            var existing = await _idempotencyStore.FindResultAsync(
                idempotencyKey,
                cancellationToken).ConfigureAwait(false);
            if (existing is not null) {
                var cached = JsonSerializer.Deserialize<OperationResult<TResult>>(existing);
                if (cached is not null) {
                    Record(cached, name, started);
                    return cached;
                }
            }
        }

        var result = await _handler.HandleAsync(command, cancellationToken)
            .ConfigureAwait(false);
        if (result.IsSuccess && command is ITransactionalApplicationCommand<TResult>) {
            if (_unitOfWork is null) {
                result = OperationResult<TResult>.Failure(
                    "application.transaction_missing",
                    "事务命令未配置工作单元。");
            }
            else {
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        if (result.IsSuccess && !string.IsNullOrWhiteSpace(idempotencyKey) &&
            _idempotencyStore is not null) {
            await _idempotencyStore.TryStoreResultAsync(
                idempotencyKey,
                JsonSerializer.Serialize(result),
                cancellationToken).ConfigureAwait(false);
        }

        Record(result, name, started);
        return result;
    }

    private void Record(
        OperationResult<TResult> result,
        string name,
        long started) {
        var elapsed = Stopwatch.GetElapsedTime(started);
        if (result.IsSuccess) {
            _telemetry.RecordSuccess(name, elapsed);
        }
        else {
            _telemetry.RecordFailure(name, result.ErrorCode, elapsed);
        }
    }
}
