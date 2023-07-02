using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace JayTom.Dws.PluginInterface {

    public interface IInitializePlugin : IPlugin {

        /// <summary>
        /// IContainerRegistry注册
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        IServiceCollection RegisterContainer(IServiceCollection service);

        /// <summary>
        /// PrismIServiceCollection注册
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        IServiceCollection RegisterPrismServices(IServiceCollection services);

        /// <summary>
        /// 程序IServiceCollection注册
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        IServiceCollection RegisterAppServices(IServiceCollection services);

        /// <summary>
        /// 初始化方法(false则不运行程序)
        /// </summary>
        /// <returns></returns>
        bool Initialize();
    }
}