using JayTom.Dws.Abstractions.Integrations;
using JayTom.Dws.Application.Integrations;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Interface;
using JayTom.Dws.Interface.CaiNiao;
using JayTom.Dws.Interface.Eshippingit;
using JayTom.Dws.Interface.geek_;
using JayTom.Dws.Interface.JdyWms;
using JayTom.Dws.Interface.Jtexpress;
using JayTom.Dws.Interface.Jushuitan;
using JayTom.Dws.Interface.Post;
using JayTom.Dws.Interface.Routdata;
using JayTom.Dws.Interface.Sunnen;
using JayTom.Dws.Interface.Szjy188;
using JayTom.Dws.Interface.ttx;
using JayTom.Dws.Interface.Wdt;
using JayTom.Dws.Interface.ZhouYi;
using JayTom.Dws.Interface.zhuoyan_scm;
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
