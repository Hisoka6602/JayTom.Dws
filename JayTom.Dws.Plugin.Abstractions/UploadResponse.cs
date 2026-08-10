namespace JayTom.Dws.PluginInterface;

/// <summary>
/// 描述插件上传调用的请求与响应元数据。
/// </summary>
public sealed class UploadResponse {
    /// <summary>获取或设置请求内容。</summary>
    public string RequestContent { get; set; } = string.Empty;
    /// <summary>获取或设置响应内容。</summary>
    public string ResponseContent { get; set; } = string.Empty;
    /// <summary>获取或设置是否成功。</summary>
    public bool IsSuccess { get; set; }
    /// <summary>获取或设置请求时间。</summary>
    public DateTimeOffset RequestTime { get; set; }
    /// <summary>获取或设置响应时间。</summary>
    public DateTimeOffset ResponseTime { get; set; }
    /// <summary>获取或设置调用耗时。</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>获取或设置已脱敏的接口参数说明。</summary>
    public string ApiParameters { get; set; } = string.Empty;
    /// <summary>获取或设置请求地址。</summary>
    public string RequestUrl { get; set; } = string.Empty;
    /// <summary>获取或设置异常说明。</summary>
    public string ExceptionMessage { get; set; } = string.Empty;
}
