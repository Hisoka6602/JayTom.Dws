using JayTom.Dws.Abstractions.Integrations;
using JayTom.Dws.Application.Integrations;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Integrations;
using JayTom.Dws.Integrations.CaiNiao;
using JayTom.Dws.Integrations.Eshippingit;
using JayTom.Dws.Integrations.geek_;
using JayTom.Dws.Integrations.JdyWms;
using JayTom.Dws.Integrations.Jtexpress;
using JayTom.Dws.Integrations.Jushuitan;
using JayTom.Dws.Integrations.Post;
using JayTom.Dws.Integrations.Routdata;
using JayTom.Dws.Integrations.Sunnen;
using JayTom.Dws.Integrations.Szjy188;
using JayTom.Dws.Integrations.ttx;
using JayTom.Dws.Integrations.Wdt;
using JayTom.Dws.Integrations.ZhouYi;
using JayTom.Dws.Integrations.zhuoyan_scm;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace JayTom.Dws.Client.Composition;

/// <summary>
/// 集中登记包裹数据上传提供商，避免工作流直接构造适配器。
/// </summary>
internal static class UploadProviderRegistration {
    /// <summary>注册全部可选上传提供商工厂。</summary>
    public static IServiceCollection AddDwsUploadProviders(this IServiceCollection services) {
        services.AddSingleton<IProviderRegistry<IDataUploader>>(provider => {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            return new ProviderRegistry<IDataUploader>(
                new Dictionary<string, Func<IDataUploader>>(StringComparer.OrdinalIgnoreCase) {
                    [ApiType.DefaultApi.ToString()] = () => new DefaultApi(httpClientFactory),
                    [ApiType.SunnenApi.ToString()] = () => new SunnenApi(httpClientFactory),
                    [ApiType.SzjyApi.ToString()] = () => new SzjyApi(httpClientFactory),
                    [ApiType.WdtWmsApi.ToString()] = () => new WdtWmsApi(httpClientFactory),
                    [ApiType.WdtErpFlagShipApi.ToString()] = () => new WdtFlagshipApi(httpClientFactory),
                    [ApiType.JdyWms.ToString()] = () => new JdyWmsApi(httpClientFactory),
                    [ApiType.JtExpressApi.ToString()] = () => new JtExpressApi(httpClientFactory),
                    [ApiType.JtPolarDayApi.ToString()] = () => new JtPolarDayApi(httpClientFactory),
                    [ApiType.RoutDataApi.ToString()] = () => new RoutDataApi(httpClientFactory),
                    [ApiType.GeekPlusApi.ToString()] = () => new GeekPlusApi(httpClientFactory),
                    [ApiType.CaiNiaoApi.ToString()] = () => new CaiNiaoApi(httpClientFactory),
                    [ApiType.EshippingitApi.ToString()] = () => new EshippingitApi(httpClientFactory),
                    [ApiType.PostApi.ToString()] = () => new PostApi(httpClientFactory),
                    [ApiType.PostInApi.ToString()] = () => new PostInApi(httpClientFactory),
                    [ApiType.ZhuoYanScm.ToString()] = () => new ZhuoYanScmApi(httpClientFactory),
                    [ApiType.TtxApi.ToString()] = () => new TtxApi(httpClientFactory),
                    [ApiType.Jushuitan.ToString()] = () => new JushuitanErpApi(httpClientFactory),
                    [ApiType.ZhouYi.ToString()] = () => new ZhouYiApi(httpClientFactory)
                });
        });
        return services;
    }
}
