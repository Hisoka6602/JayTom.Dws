namespace JayTom.Dws.Plugin.Runtime;

/// <summary>描述插件发现或加载失败的稳定分类。</summary>
public enum PluginLoadStatus
{
    /// <summary>插件签名、摘要或权限不受信任。</summary>
    UntrustedPackage = -1,

    /// <summary>插件清单无法读取或内容无效。</summary>
    InvalidManifest,

    /// <summary>插件版本与当前宿主不兼容。</summary>
    Incompatible,

    /// <summary>插件稳定标识与已加载插件重复。</summary>
    DuplicateKey,

    /// <summary>插件入口点格式无效或越过插件目录边界。</summary>
    InvalidEntryPoint,

    /// <summary>入口程序集不存在。</summary>
    AssemblyNotFound,

    /// <summary>入口类型不存在。</summary>
    TypeNotFound,

    /// <summary>入口类型未实现公共插件契约。</summary>
    ContractMismatch,

    /// <summary>插件实例创建或初始化失败。</summary>
    ActivationFailed
}
