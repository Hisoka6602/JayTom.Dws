// DWS-COHESIVE-CONTRACTS: 遥测端口与其无后端默认实现共同构成可选能力。
namespace JayTom.Dws.Application.UseCases;

/// <summary>抽象应用用例的日志、指标和关联跟踪。</summary>
public interface IApplicationUseCaseTelemetry {
    /// <summary>开始一次用例观测。</summary>
    IDisposable Begin(string useCaseName, string? idempotencyKey);

    /// <summary>记录用例成功。</summary>
    void RecordSuccess(string useCaseName, TimeSpan elapsed);

    /// <summary>记录预期失败。</summary>
    void RecordFailure(string useCaseName, string errorCode, TimeSpan elapsed);
}

/// <summary>提供无需外部遥测后端的空实现。</summary>
internal sealed class NullApplicationUseCaseTelemetry : IApplicationUseCaseTelemetry {
    /// <summary>获取共享实例。</summary>
    public static NullApplicationUseCaseTelemetry Instance { get; } = new();

    /// <inheritdoc />
    public IDisposable Begin(string useCaseName, string? idempotencyKey) => NoopScope.Instance;

    /// <inheritdoc />
    public void RecordSuccess(string useCaseName, TimeSpan elapsed) { }

    /// <inheritdoc />
    public void RecordFailure(string useCaseName, string errorCode, TimeSpan elapsed) { }

    private sealed class NoopScope : IDisposable {
        public static NoopScope Instance { get; } = new();
        public void Dispose() { }
    }
}
