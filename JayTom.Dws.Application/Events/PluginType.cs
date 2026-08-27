namespace JayTom.Dws.Application.Events;

/// <summary>定义兼容插件配置所覆盖的扩展位置。</summary>
public enum PluginType
{
    /// <summary>扩展包。</summary>
    ExtensionPackage,
    /// <summary>主页。</summary>
    Home,
    /// <summary>内页。</summary>
    Inner,
    /// <summary>弹窗。</summary>
    Dialog,
    /// <summary>控件。</summary>
    Control,
    /// <summary>工具。</summary>
    Tool,
    /// <summary>上传接口。</summary>
    Api,
    /// <summary>过滤逻辑。</summary>
    Filter,
    /// <summary>处理逻辑。</summary>
    Process,
    /// <summary>初始化插件。</summary>
    Initialize,
    /// <summary>后台处理。</summary>
    Background,
    /// <summary>设备。</summary>
    Device,
    /// <summary>主页工具。</summary>
    HomeTool
}
