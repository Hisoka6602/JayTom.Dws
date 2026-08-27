using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.Sorting;
using JayTom.Dws.Legacy.Contracts.DownstreamProtocols;

namespace JayTom.Dws.Client.Service.Sorting;

/// <summary>把应用层逻辑指令适配到现有下位机连接服务。</summary>
internal sealed class SortingConnectionProtocolAdapter : ISortingProtocolAdapter
{
    /// <summary>下位机连接服务。</summary>
    private readonly ISortingConnectionService _connectionService;

    /// <summary>用于生成可测试回执时间的时间提供程序。</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>创建下位机协议适配器。</summary>
    public SortingConnectionProtocolAdapter(
        ISortingConnectionService connectionService,
        TimeProvider timeProvider)
    {
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>验证连接状态并把稳定命令转换为旧协议附加信息。</summary>
    public Task<OperationResult<SortingDispatchReceipt>> SendAsync(
        SortingProtocolCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_connectionService.IsConnected)
        {
            return Task.FromResult(OperationResult<SortingDispatchReceipt>.Failure(
                "sorting.connection_unavailable",
                "没有可用的下位机连接。"));
        }

        try
        {
            List<string> instructions = command.Instructions is List<string> instructionList
                ? instructionList
                : [.. command.Instructions];
            var attachment = new InstructionsAttach
            {
                Guid = command.PackageId,
                BarCode = command.Barcode
            };
            _connectionService.SendInstructions(
                command,
                command.ExitId,
                instructions,
                command.Interval,
                attachment);
            return Task.FromResult(OperationResult<SortingDispatchReceipt>.Success(
                new SortingDispatchReceipt(
                    command.PackageId,
                    command.ExitId,
                    _timeProvider.GetUtcNow())));
        }
        catch (Exception exception)
        {
            return Task.FromResult(OperationResult<SortingDispatchReceipt>.Failure(
                "sorting.protocol_failure",
                exception.Message));
        }
    }
}
