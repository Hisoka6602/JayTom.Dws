using System;
using Microsoft.Extensions.Hosting;

namespace JayTom.Dws.Client.Service.Runtime;

/// <summary>描述由监督器负责创建、启动、重启和释放的后台服务。</summary>
/// <param name="ServiceType">后台服务的具体类型。</param>
internal sealed record SupervisedHostedServiceDescriptor(Type ServiceType) {
    /// <summary>创建指定后台服务类型的描述。</summary>
    public static SupervisedHostedServiceDescriptor Create<TService>()
        where TService : class, IHostedService =>
        new(typeof(TService));
}
