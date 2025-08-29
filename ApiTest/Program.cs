using System.Drawing;
using JayTom.Dws.Interface;
using JayTom.Dws.Interface.ApiImplementations.geek_;

internal class Program {
    private static async Task Main(string[] args) {
        Console.WriteLine("Hello, World!");

        Host.CreateDefaultBuilder().ConfigureServices((builder, services) => {
            services.AddHttpClient("INSURANCE", httpClient => {
                // httpClient.Timeout = TimeSpan.FromSeconds(10);
            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler() {
                    UseDefaultCredentials = true,
                    MaxConnectionsPerServer = 1000,
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true,
                    //UseProxy = false,
                    //SslProtocols = SslProtocols.Tls13
                };

                return handler;
            });
            services.AddHostedService<Worker>();
            services.AddScoped<IDataUploader, TtxApi>();
            //services.AddScoped<IPackageUpload, WdtUltimateApi>();
        }).Build().Run();
    }
}

public class Worker : BackgroundService {
    private readonly IDataUploader _dataUploader;

    public Worker(IDataUploader dataUploader) {
        _dataUploader = dataUploader;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var uploadData = await _dataUploader.UploadData("9876418933477", 1.2, token: stoppingToken);
        return;
    }
}