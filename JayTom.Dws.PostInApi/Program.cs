using NLog.Web;
using Newtonsoft.Json;
using System.Text.Unicode;
using NLog.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;

internal class Program {

    private static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        //注入
        builder.WebHost.UseKestrel((context, options) => {
            // 设置应用服务器 Kestrel 请求体最大为50MB
            options.Limits.MaxRequestBodySize = 31457280000;
        });
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

        //取消unicode
        builder.Services.AddControllersWithViews().AddJsonOptions(options => {
            options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        });
        //配置内存缓存
        builder.Services.AddMemoryCache();
        //字节限制
        builder.Services.Configure<FormOptions>(options => {
            options.MultipartBodyLengthLimit = long.MaxValue;
            //缓存请求正文
            //options.BufferBody = true;
        });

        //异常处理过滤器/中间件可以不添加，看情况
        builder.Services.Configure<ApiBehaviorOptions>(opt => {
            opt.InvalidModelStateResponseFactory = actionContext => {
                //获取验证失败的模型字段
                var errors = actionContext.ModelState.Where(w => w.Value?.Errors.Count > 0)?.Select(s =>
                    $"[{s.Key}]:{s.Value?.Errors?.FirstOrDefault()?.ErrorMessage}")?.ToList();
                return new JsonResult(new { Result = false, Msg = $"Body:{string.Join("|", errors ?? new List<string>())}" });
            };
        });
        //跨域设置
        builder.Services.AddCors(options => options.AddPolicy("CorsPolicy",
            policyBuilder => {
                policyBuilder.AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(_ => true) // =AllowAnyOrigin()
                    .AllowCredentials();
                options.AddPolicy("SignalR",
                    corsPolicyBuilder => {
                        corsPolicyBuilder.AllowAnyMethod()
                            .AllowAnyHeader()
                            .SetIsOriginAllowed(str => true)
                            .AllowCredentials();
                    });
            }));
        builder.Services.AddControllers().AddNewtonsoftJson(options => {
            // 格式化返回 JSON
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local; // 设置时区为 UTC
            options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
        });
        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        //Body重用
        app.Use(next => async context => {
            context.Request.EnableBuffering();
            await next.Invoke(context);
        });
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}