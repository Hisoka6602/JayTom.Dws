using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.Sorting;

namespace JayTom.Dws.Tests.TestDoubles;

/// <summary>记录管道提交命令的协议适配器替身。</summary>
internal sealed class RecordingSortingProtocolAdapter : ISortingProtocolAdapter
{
    /// <summary>获取最近一次收到的命令。</summary>
    public SortingProtocolCommand? Command { get; private set; }

    /// <summary>获取或设置适配器返回的错误。</summary>
    public string FailureCode { get; set; } = string.Empty;

    /// <summary>记录命令并返回可预测回执。</summary>
    public Task<OperationResult<SortingDispatchReceipt>> SendAsync(
        SortingProtocolCommand command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Command = command;
        OperationResult<SortingDispatchReceipt> result = string.IsNullOrEmpty(FailureCode)
            ? OperationResult<SortingDispatchReceipt>.Success(
                new SortingDispatchReceipt(command.PackageId, command.ExitId, DateTimeOffset.UnixEpoch))
            : OperationResult<SortingDispatchReceipt>.Failure(FailureCode, "协议拒绝命令。");
        return Task.FromResult(result);
    }
}
