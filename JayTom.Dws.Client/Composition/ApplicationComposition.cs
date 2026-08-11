using DryIoc;
using DryIoc.Microsoft.DependencyInjection.Extension;
using Prism.Ioc;
using Prism.DryIoc;
using JayTom.Dws.Infrastructure.DependencyInjection;

namespace JayTom.Dws.Client.Composition;

/// <summary>组织桌面端各模块的依赖注册。</summary>
public static class ApplicationComposition {
    /// <summary>注册界面、持久化、适配器、应用服务与后台工作流。</summary>
    public static void Register(IContainerRegistry registry) {
        registry.GetContainer().Rules.WithoutThrowOnRegisteringDisposableTransient();
        registry.RegisterPresentation();
        registry.GetContainer().RegisterServices(services => {
            services.AddDwsPersistence();
            services.AddDwsPlatformAdapters();
            services.AddDwsApplicationServices();
            services.AddDwsHostedWorkflows();
        });
    }
}
