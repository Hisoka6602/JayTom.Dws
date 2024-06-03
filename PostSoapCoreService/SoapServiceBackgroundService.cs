using SoapCore;
using Microsoft.AspNetCore.Http;
using PostSoapCoreService.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace PostSoapCoreService {

    public class SoapServiceBackgroundService : BackgroundService {
        private readonly ILogger<SoapServiceBackgroundService> _logger;
        private readonly IOptions<SoapServiceSettings> _soapServiceSettings;

        public SoapServiceBackgroundService(IServiceProvider serviceProvider, ILogger<SoapServiceBackgroundService> logger,
            IOptions<SoapServiceSettings> soapServiceSettings) {
            _logger = logger;
            _soapServiceSettings = soapServiceSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseKestrel()
                        .UseUrls(_soapServiceSettings.Value.BaseUrl)
                        .ConfigureServices(services => {
                            services.AddSoapCore();
                            services.AddSingleton<ICommFjjService, CommFjjService>();
                            services.AddMvc();
                        })
                        .Configure(app => {
                            app.UseRouting();
                            app.UseEndpoints(endpoints => {
                                endpoints.UseSoapEndpoint<ICommFjjService>("/FjjService/services/CommFJJ", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);

                                /*
                                // 添加用于处理 WSDL 请求的路由处理程序
                                endpoints.MapGet("/FjjService/services/CommFJJ", async context => {
                                    // 返回 WSDL 描述信息
                                    context.Response.ContentType = "text/xml";
                                    await context.Response.WriteAsync(/* WSDL 描述信息内容 #1#string.Empty, cancellationToken: stoppingToken);
                                });*/
                            });
                        });
                });

            var host = hostBuilder.Build();
            await host.RunAsync(stoppingToken);
        }
    }
}