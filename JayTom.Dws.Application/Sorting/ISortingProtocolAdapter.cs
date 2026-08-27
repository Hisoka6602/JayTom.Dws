using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Sorting;

/// <summary>隔离应用层分拣命令与具体厂商通讯协议。</summary>
public interface ISortingProtocolAdapter
{
    /// <summary>把逻辑命令转换并提交给实际通讯通道。</summary>
    Task<OperationResult<SortingDispatchReceipt>> SendAsync(
        SortingProtocolCommand command,
        CancellationToken cancellationToken);
}
