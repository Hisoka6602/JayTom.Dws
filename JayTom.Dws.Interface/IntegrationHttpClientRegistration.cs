using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace JayTom.Dws.Integrations;

/// <summary>提供外部接口命名客户端的唯一组合入口。</summary>
public static class IntegrationHttpClientRegistration
{
    /// <summary>注册统一的连接池、超时、安全重试与熔断策略。</summary>
    public static IServiceCollection AddDwsIntegrationHttpClient(
        this IServiceCollection services,
        IntegrationResilienceOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        IntegrationResilienceOptions effectiveOptions = options ?? IntegrationResilienceOptions.Default;
        effectiveOptions.Validate();

        services.AddSingleton(effectiveOptions);
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<IntegrationResilienceHandler>();
        services.AddHttpClient(
                ApiHttpClientNames.ExternalApi,
                client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                MaxConnectionsPerServer = 600,
                ConnectTimeout = TimeSpan.FromSeconds(15),
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                AutomaticDecompression = DecompressionMethods.GZip |
                                         DecompressionMethods.Deflate |
                                         DecompressionMethods.Brotli
            })
            .AddHttpMessageHandler<IntegrationResilienceHandler>();
        return services;
    }
}
