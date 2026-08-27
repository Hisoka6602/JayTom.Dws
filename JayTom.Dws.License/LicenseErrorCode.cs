namespace JayTom.Dws.License;

/// <summary>
/// 定义稳定的授权签发和校验错误码。
/// </summary>
public enum LicenseErrorCode {
    /// <summary>操作成功。</summary>
    None = 0,
    /// <summary>找不到授权文件。</summary>
    FileNotFound,
    /// <summary>授权文件超过长度限制。</summary>
    FileTooLarge,
    /// <summary>授权格式不受支持。</summary>
    UnsupportedFormat,
    /// <summary>授权载荷无效。</summary>
    InvalidPayload,
    /// <summary>RSA 密钥无效。</summary>
    InvalidKey,
    /// <summary>缺少信任根。</summary>
    TrustRootMissing,
    /// <summary>签名密钥不受信任。</summary>
    UntrustedKey,
    /// <summary>签名密钥已吊销。</summary>
    RevokedKey,
    /// <summary>签名无效。</summary>
    InvalidSignature,
    /// <summary>机器绑定不匹配。</summary>
    MachineMismatch,
    /// <summary>授权已过期。</summary>
    Expired,
    /// <summary>授权已冻结。</summary>
    RevokedLicense,
    /// <summary>检测到时钟回拨。</summary>
    ClockRollback,
    /// <summary>签发过程失败。</summary>
    SigningFailed
}
