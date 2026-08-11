using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 定义插件初始化与服务声明能力，不允许插件直接修改宿主容器。
/// </summary>
public interface IInitializePlugin : IPlugin {
    /// <summary>获取插件请求注册的服务描述。</summary>
    IReadOnlyCollection<PluginServiceRegistration> Services { get; }

    /// <summary>执行插件自身初始化。</summary>
    Result Initialize();
}
