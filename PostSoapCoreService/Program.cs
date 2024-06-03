using NLog.Web;
using PostSoapCoreService;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Configuration;

internal class Program {

    private static void Main(string[] args) {
        /*// 加载NLog配置
        var nlogConfig = new ConfigurationBuilder()
            .AddJsonFile("nlog.config", optional: false, reloadOnChange: true)
            .Build();

        // 配置NLog
        NLog.LogManager.Configuration = new NLogLoggingConfiguration(nlogConfig);

        // 手动启动NLog
        var logger = NLog.LogManager.GetCurrentClassLogger();*/
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => {
        services.AddHostedService<SoapServiceBackgroundService>();

        // 添加配置
        services.Configure<SoapServiceSettings>(configuration.GetSection("SoapServiceSettings"));
    })
    .UseWindowsService() // 配置为Windows服务
    .ConfigureLogging(logging => {
        logging.ClearProviders();
        logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    }).UseNLog()
    .Build();

        host.Run();
    }
}