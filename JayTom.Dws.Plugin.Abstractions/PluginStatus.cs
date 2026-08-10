namespace JayTom.Dws.PluginInterface;

/// <summary>
/// 描述插件安装与可用状态。
/// </summary>
public enum PluginStatus {
    /// <summary>尚未安装。</summary>
    NotInstalled,
    /// <summary>已经安装。</summary>
    Installed,
    /// <summary>存在可用升级。</summary>
    Upgradeable,
    /// <summary>插件无效。</summary>
    Invalid,
    /// <summary>插件存在已知缺陷。</summary>
    BugFound
}
