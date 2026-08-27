using System.Threading;
using System.Threading.Tasks;

namespace JayTom.Dws.Client.Service.Runtime;

/// <summary>协调桌面应用配置、后台服务、设备与分拣组件的启动和停机顺序。</summary>
public interface IApplicationLifecycleCoordinator
{
    /// <summary>初始化配置并按依赖顺序启动后台工作流。</summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>按依赖逆序停止生产者与后台消费者。</summary>
    Task StopAsync(CancellationToken cancellationToken);
}
