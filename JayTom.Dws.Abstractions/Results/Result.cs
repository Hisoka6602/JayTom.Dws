namespace JayTom.Dws.Abstractions.Results;

/// <summary>
/// 表示不依赖布尔值与附加对象组合的操作结果。
/// </summary>
public readonly record struct Result {
    /// <summary>创建具有指定成功状态与错误的结果。</summary>
    private Result(bool isSuccess, Error error) {
        if (isSuccess == (error != Error.None)) {
            throw new ArgumentException("Successful results cannot contain an error and failed results must contain one.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>获取操作是否成功。</summary>
    public bool IsSuccess { get; }

    /// <summary>获取操作是否失败。</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>获取失败信息。</summary>
    public Error Error { get; }

    /// <summary>创建成功结果。</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>创建失败结果。</summary>
    public static Result Failure(Error error) => new(false, error);
}
