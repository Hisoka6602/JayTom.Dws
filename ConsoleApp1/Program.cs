using System.Drawing;
using JayTom.Dws.Interface;
using Microsoft.Extensions.Hosting;
using JayTom.Dws.Interface.WeciMexicoDv;
using Microsoft.Extensions.DependencyInjection;

internal class Program {

    private static void Main(string[] args) {
        Host.CreateDefaultBuilder().ConfigureServices((builder, services) => {
            //var config = builder.Configuration;

            /*services.AddHttpClient("INSURANCE", option => {
                //option.Timeout = TimeSpan.FromSeconds(10);
            }).ConfigureHttpMessageHandlerBuilder(builder => {
                builder.PrimaryHandler = new HttpClientHandler {
                    UseDefaultCredentials = true,
                    MaxConnectionsPerServer = 1000,
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true,
                    UseProxy = false,
                };
            });*/
            services.AddHttpClient("INSURANCE", httpClient => {
                // httpClient.Timeout = TimeSpan.FromSeconds(10);
            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler() {
                    UseDefaultCredentials = true,
                    MaxConnectionsPerServer = 1000,
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true,
                    UseProxy = false
                };

                return handler;
            });
            services.AddHostedService<Worker>();
            services.AddScoped<IDataUploader, WeciMexicoDvApi>();
            //services.AddScoped<IPackageUpload, WdtUltimateApi>();
        }).Build().Run();
        Console.WriteLine("Hello, World!");
    }

    public class Worker : BackgroundService {
        private readonly IDataUploader _dataUploader;

        public Worker(IDataUploader dataUploader) {
            _dataUploader = dataUploader;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var fromFile = Image.FromFile($"{AppDomain.CurrentDomain.BaseDirectory}12.jpg");
            var uploadResponse = await _dataUploader.UploadData("00452712", 36.8, 2.2, 4.5,
                6.2, image: fromFile, token: stoppingToken);

            Console.WriteLine(uploadResponse);
        }
    }
}