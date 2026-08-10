namespace JayTom.Dws.PluginInterface;

/// <summary>
/// 描述插件消息请求的动作。
/// </summary>
public enum PluginActionType {
    /// <summary>发送。</summary>
    Send,
    /// <summary>取消。</summary>
    Cancel,
    /// <summary>跳转。</summary>
    Redirect,
    /// <summary>上传。</summary>
    Upload,
    /// <summary>关闭。</summary>
    Close,
    /// <summary>加载。</summary>
    Load,
    /// <summary>删除。</summary>
    Delete,
    /// <summary>展示。</summary>
    Show
}
