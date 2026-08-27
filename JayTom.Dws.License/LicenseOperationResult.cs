namespace JayTom.Dws.License;

/// <summary>
/// 表示一次授权签发或校验操作的结构化结果。
/// </summary>
/// <param name="IsSuccess">操作是否成功。</param>
/// <param name="ErrorCode">稳定错误码。</param>
/// <param name="Message">面向用户的说明。</param>
public sealed record LicenseOperationResult(
    bool IsSuccess,
    LicenseErrorCode ErrorCode,
    string Message) {
    /// <summary>创建成功结果。</summary>
    public static LicenseOperationResult Success(string message) {
        return new LicenseOperationResult(true, LicenseErrorCode.None, message);
    }

    /// <summary>创建失败结果。</summary>
    public static LicenseOperationResult Failure(
        LicenseErrorCode errorCode,
        string message) {
        return new LicenseOperationResult(false, errorCode, message);
    }
}
