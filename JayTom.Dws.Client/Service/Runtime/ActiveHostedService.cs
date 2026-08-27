using Microsoft.Extensions.Hosting;

namespace JayTom.Dws.Client.Service.Runtime;

/// <summary>保存当前由监督器拥有的后台服务实例。</summary>
/// <param name="Service">当前正在启动或运行的服务。</param>
internal sealed record ActiveHostedService(IHostedService Service);
