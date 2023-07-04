using System.Drawing;
using JayTom.Dws.Utils;
using JayTom.Dws.Interface;
using System.Drawing.Imaging;
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
            /*var fromFile = Image.FromFile($"{AppDomain.CurrentDomain.BaseDirectory}12.jpg");

            var imageToBase64 = fromFile.ConvertImageToBase64();
            var image = imageToBase64.ConvertBase64ToImage();
            image.Save($"{AppDomain.CurrentDomain.BaseDirectory}13.jpg", ImageFormat.Jpeg);

            return;*/
            var (key, value) = await _dataUploader.SetParameters(new WeciMexicoDvApiParam {
                Url = "https://us-central1-ivoy-warehouse.cloudfunctions.net/weighing-machine",
                TimeOut = 100000
            });
            var uploadResponse = await _dataUploader.UploadData("NM1303QT811B8CAYITRUCK0", 0.1, 2.2, 4.5,
                0.1, token: stoppingToken);

            Console.WriteLine(uploadResponse);
        }
    }
}