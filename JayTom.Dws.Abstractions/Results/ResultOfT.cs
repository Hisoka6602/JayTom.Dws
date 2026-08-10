namespace JayTom.Dws.Abstractions.Results;

/// <summary>
/// 表示带有强类型成功值的操作结果。
/// </summary>
public readonly record struct Result<T> {
    /// <summary>成功值的内部存储。</summary>
    private readonly T? _value;

    /// <summary>创建包含成功值的结果。</summary>
    private Result(T value) {
        _value = value;
        Error = Error.None;
        IsSuccess = true;
    }

    /// <summary>创建包含错误的失败结果。</summary>
    private Result(Error error) {
        if (error == Error.None) {
            throw new ArgumentException("Failed results must contain an error.", nameof(error));
        }

        _value = default;
        Error = error;
        IsSuccess = false;
    }

    /// <summary>获取操作是否成功。</summary>
    public bool IsSuccess { get; }

    /// <summary>获取操作是否失败。</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>获取失败信息。</summary>
    public Error Error { get; }

    /// <summary>获取成功值；失败结果读取时会抛出异常。</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    /// <summary>创建包含指定值的成功结果。</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>创建失败结果。</summary>
    public static Result<T> Failure(Error error) => new(error);
}
