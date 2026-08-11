namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 封装插件运行异常。
/// </summary>
public sealed class PluginExceptionEventArgs : EventArgs {
    /// <summary>初始化插件异常事件。</summary>
    public PluginExceptionEventArgs(Exception exception) {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>获取插件异常。</summary>
    public Exception Exception { get; }
}
