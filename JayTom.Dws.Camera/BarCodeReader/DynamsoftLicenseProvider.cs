namespace JayTom.Dws.Camera.BarCodeReader;

/// <summary>
/// 从进程环境读取 Dynamsoft 许可证，避免将凭据编译进程序集或提交到源代码仓库。
/// </summary>
internal static class DynamsoftLicenseProvider
{
    /// <summary>条码识别许可证的环境变量名。</summary>
    internal const string BarcodeReaderEnvironmentVariable = "DWS_DYNAMSOFT_DBR_LICENSE";

    /// <summary>USB 相机许可证的环境变量名。</summary>
    internal const string CameraEnvironmentVariable = "DWS_DYNAMSOFT_DNT_LICENSE";

    /// <summary>读取必需的许可证；未配置时安全失败。</summary>
    internal static string GetRequired(string environmentVariableName)
    {
        string? license = Environment.GetEnvironmentVariable(environmentVariableName);
        return !string.IsNullOrWhiteSpace(license)
            ? license
            : throw new InvalidOperationException(
                $"缺少必需的许可证环境变量 {environmentVariableName}。");
    }
}
