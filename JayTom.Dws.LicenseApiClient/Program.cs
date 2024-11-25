using NLog;
using NLog.Web;
using Newtonsoft.Json;
using MudBlazor.Services;
using NLog.Extensions.Logging;
using JayTom.Dws.LicenseApiClient.Api;
using JayTom.Dws.LicenseApiClient.Data;
using Microsoft.AspNetCore.Diagnostics;
using JayTom.Dws.LicenseApiClient.Notification;
using JayTom.Dws.LicenseApiClient.Plugin.Excel;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace JayTom.Dws.LicenseApiClient;

internal class Program {

    private static void Main(string[] args) {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
            var exception = args.ExceptionObject as Exception;
            LogManager.GetCurrentClassLogger().Log(NLog.LogLevel.Error, $"Unhandled Exception: {exception}");
        };

        TaskScheduler.UnobservedTaskException += (sender, args) => {
            LogManager.GetCurrentClassLogger().Log(NLog.LogLevel.Error, $"Unobserved Task Exception: {args.Exception}");
            args.SetObserved();
        };

        var builder = WebApplication.CreateBuilder(args);

        //配置从配置文件的`NLog` 节点读取配置
        var nlogConfig = builder.Configuration.GetSection("NLog");
        NLog.LogManager.Configuration = new NLogLoggingConfiguration(nlogConfig);
        //清空其他日志Providers
        builder.Logging.ClearProviders();
        //最小记录等级
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        //该配置用来指定使用ASP.NET Core 默认的日志过滤器
        var nlogOptions = new NLogAspNetCoreOptions() { RemoveLoggerFactoryFilter = false };
        builder.Host.UseNLog(nlogOptions); //启用NLog

        builder.Services.AddMudServices();

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddSingleton<WeatherForecastService>();
        builder.Services.AddHttpClient("INSURANCE", httpClient => {
            // httpClient.Timeout = TimeSpan.FromSeconds(10);
        }).ConfigurePrimaryHttpMessageHandler(() => {
            var handler = new HttpClientHandler() {
                UseDefaultCredentials = true,
                MaxConnectionsPerServer = 600,
                ServerCertificateCustomValidationCallback = (m, c, ch, _) => true,
                //UseProxy = false
            };

            return handler;
        });
        //接口注入
        builder.Services.AddScoped<ILicenseApiRequest, LicenseApiRequest>();
        //插件注入
        builder.Services.AddSingleton<IExcelService, NpoiExport>();
        //订阅事件
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddOptions();
        builder.Services.AddAuthorizationCore();
        var app = builder.Build();
        app.UseExceptionHandler(config => {
            config.Run(async context => {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var error = context.Features.Get<IExceptionHandlerFeature>();
                {
                    var ex = error?.Error;
                    LogManager.GetCurrentClassLogger().Log(NLog.LogLevel.Error, $"系统异常:{ex}");
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new { Result = false, Msg = "系统异常" }));
                }
            });
        });
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}