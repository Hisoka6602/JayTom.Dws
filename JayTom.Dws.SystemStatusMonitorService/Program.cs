using NLog.Web;
using System.Text;
using System.Configuration;
using NLog.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using JayTom.Dws.CrossCutting.SignalR;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.SystemStatusMonitorService.Service;
using JayTom.Dws.SystemStatusMonitorService.SignalR;

namespace JayTom.Dws.SystemStatusMonitorService;

internal class Program {
    private static MonitorServiceSettings _settings = new();

    private static void Main(string[] args) {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        _settings = configuration.GetSection(nameof(MonitorServiceSettings)).Get<MonitorServiceSettings>() ?? new MonitorServiceSettings();
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder => {
                webBuilder.UseKestrel()
                    .UseUrls(_settings.BaseUrl)
                    .ConfigureServices((hostContext, services) => {
                        services.AddSingleton<IComputer, Computer>();
                        services.AddSingleton<IBaseServerMessageHub, BaseServerMessageHub>();
                        services.AddSingleton<ISystemStatusMonitorMessageHub, SystemStatusMonitorMessageHub>();
                        services.AddMvc();

                        // 添加 SignalR 服务
                        services.AddSignalR(options => {
                            options.HandshakeTimeout = TimeSpan.FromMinutes(1);
                            options.EnableDetailedErrors = true;
                            options.MaximumReceiveMessageSize = null;
                            options.KeepAliveInterval = TimeSpan.FromMinutes(1);
                            options.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
                            options.MaximumParallelInvocationsPerClient = 10;
                            options.StreamBufferCapacity = int.MaxValue;
                        });
                    })
                    .Configure(app => {
                        app.UseRouting();

                        // 配置 SignalR Hub 终结点
                        app.UseEndpoints(endpoints => {
                            endpoints.MapHub<BaseServerMessageHub>("/Message", options => {
                                options.TransportMaxBufferSize = 0;
                                options.ApplicationMaxBufferSize = 0;
                                options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(10);
                            });

                            // 可以添加其他端点配置
                        });
                    });
            })
            .ConfigureServices(services => {
                services.AddHostedService<Worker>();
            })
            .UseWindowsService() // 配置为Windows服务
            .ConfigureLogging(logging => {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddNLog();
            })
            .UseNLog();

        var host = hostBuilder.Build();
        host.Run();
    }
}