using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JayTom.Dws.Client.Service.Runtime;

/// <summary>
/// 统一管理桌面应用后台服务的启动、故障重启、健康状态和停止。
/// </summary>
public interface IHostedServiceSupervisor {
    /// <summary>启动并开始监督全部后台服务。</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>同步建立停机屏障，阻止已结束的服务在正式停止前再次启动。</summary>
    void BeginShutdown();

    /// <summary>停止监督并按逆序停止全部后台服务。</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>获取当前后台服务健康状态快照。</summary>
    IReadOnlyDictionary<string, string> GetHealthSnapshot();
}
