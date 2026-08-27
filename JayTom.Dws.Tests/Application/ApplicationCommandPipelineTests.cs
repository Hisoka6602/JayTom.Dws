using JayTom.Dws.Abstractions.Persistence;
using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.UseCases;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证应用命令横切管道的校验、幂等和事务提交顺序。</summary>
public sealed class ApplicationCommandPipelineTests {
    /// <summary>同一幂等键重复执行时只应处理并提交一次。</summary>
    [Fact]
    public async Task Idempotent_transactional_command_executes_and_commits_once() {
        var handler = new CountingHandler();
        var unitOfWork = new CountingUnitOfWork();
        var store = new InMemoryIdempotencyStore();
        var pipeline = new ApplicationCommandPipeline<TestCommand, string>(
            handler,
            [],
            unitOfWork: unitOfWork,
            idempotencyStore: store);
        var command = new TestCommand("value", "request-001");

        var first = await pipeline.ExecuteAsync(command);
        var second = await pipeline.ExecuteAsync(command);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal("VALUE", second.Value);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    /// <summary>校验失败应在处理器、事务和幂等存储之前短路。</summary>
    [Fact]
    public async Task Validation_failure_short_circuits_all_side_effects() {
        var handler = new CountingHandler();
        var unitOfWork = new CountingUnitOfWork();
        var store = new InMemoryIdempotencyStore();
        var pipeline = new ApplicationCommandPipeline<TestCommand, string>(
            handler,
            [new RejectingValidator()],
            unitOfWork: unitOfWork,
            idempotencyStore: store);

        var result = await pipeline.ExecuteAsync(new TestCommand("", "request-002"));

        Assert.False(result.IsSuccess);
        Assert.Equal("test.invalid", result.ErrorCode);
        Assert.Equal(0, handler.CallCount);
        Assert.Equal(0, unitOfWork.SaveCount);
        Assert.Empty(store.Results);
    }

    private sealed record TestCommand(string Value, string IdempotencyKey) :
        IIdempotentApplicationCommand<string>,
        ITransactionalApplicationCommand<string>;

    private sealed class CountingHandler :
        IApplicationCommandHandler<TestCommand, string> {
        public int CallCount { get; private set; }

        public Task<OperationResult<string>> HandleAsync(
            TestCommand command,
            CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(OperationResult<string>.Success(
                command.Value.ToUpperInvariant()));
        }
    }

    private sealed class RejectingValidator : IApplicationRequestValidator<TestCommand> {
        public IReadOnlyList<Error> Validate(TestCommand request) =>
            [new Error("test.invalid", "The command is invalid.")];
    }

    private sealed class CountingUnitOfWork : IUnitOfWork {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class InMemoryIdempotencyStore : IApplicationIdempotencyStore {
        public Dictionary<string, string> Results { get; } = new(StringComparer.Ordinal);

        public Task<string?> FindResultAsync(
            string key,
            CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            Results.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task<bool> TryStoreResultAsync(
            string key,
            string serializedResult,
            CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Results.TryAdd(key, serializedResult));
        }
    }
}
