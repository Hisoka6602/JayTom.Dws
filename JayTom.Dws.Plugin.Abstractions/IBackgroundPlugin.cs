using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 定义不依赖特定宿主框架的后台插件生命周期。
/// </summary>
public interface IBackgroundPlugin : IPlugin {
    /// <summary>启动后台插件。</summary>
    Task<Result> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>停止后台插件。</summary>
    Task<Result> StopAsync(CancellationToken cancellationToken = default);
}
