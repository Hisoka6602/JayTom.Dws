namespace JayTom.Dws.Abstractions.Results;

/// <summary>表示具有明确成功状态、数据和错误信息的操作结果。</summary>
/// <typeparam name="TValue">成功时返回的数据类型。</typeparam>
public sealed record OperationResult<TValue> {
    /// <summary>获取操作是否成功。</summary>
    public bool IsSuccess { get; init; }

    /// <summary>获取成功时返回的数据。</summary>
    public TValue? Value { get; init; }

    /// <summary>获取稳定的错误代码。</summary>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>获取供用户或日志显示的错误信息。</summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>创建成功结果。</summary>
    /// <param name="value">成功数据。</param>
    /// <returns>成功结果。</returns>
    public static OperationResult<TValue> Success(TValue value) => new() {
        IsSuccess = true,
        Value = value
    };

    /// <summary>创建失败结果。</summary>
    /// <param name="errorCode">稳定错误代码。</param>
    /// <param name="errorMessage">错误信息。</param>
    /// <returns>失败结果。</returns>
    public static OperationResult<TValue> Failure(string errorCode, string errorMessage) => new() {
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };

    /// <summary>创建同时携带兼容数据的失败结果。</summary>
    /// <param name="errorCode">稳定错误代码。</param>
    /// <param name="errorMessage">错误信息。</param>
    /// <param name="value">供现有调用方读取的失败数据。</param>
    /// <returns>失败结果。</returns>
    public static OperationResult<TValue> Failure(
        string errorCode,
        string errorMessage,
        TValue? value) => new() {
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        Value = value
    };

    /// <summary>兼容现有解构调用，并公开具有名称的成功状态和数据。</summary>
    /// <param name="isSuccess">操作是否成功。</param>
    /// <param name="value">成功数据；失败时为默认值。</param>
    public void Deconstruct(out bool isSuccess, out TValue? value) {
        isSuccess = IsSuccess;
        value = Value;
        if (!isSuccess && value is null && typeof(TValue) == typeof(string)) {
            value = (TValue)(object)ErrorMessage;
        }
    }
}
