using System.Drawing;
using JayTom.Dws.Utils;
using JayTom.Dws.Interface;
using System.Drawing.Imaging;
using Microsoft.Extensions.Hosting;
using System.Net.NetworkInformation;
using System.Security.Authentication;
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
                    //UseProxy = false,
                    //SslProtocols = SslProtocols.Tls13
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
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in interfaces) {
                Console.WriteLine($"Interface Name: {networkInterface.Name}");
                Console.WriteLine($"Speed: {networkInterface.Speed} bytes per second");
                Console.WriteLine();
            }

            return;

            var fromFile = Image.FromFile($"{AppDomain.CurrentDomain.BaseDirectory}17.jpg");

            /*var watermark = fromFile.AddTextWatermark($"SF123456789\n0.33\ntime:20230707", Color.Blue);
            watermark.Save($"{AppDomain.CurrentDomain.BaseDirectory}watermark.jpg", ImageFormat.Jpeg);
            var imageToBase64 = fromFile.ConvertImageToBase64();
            var image = imageToBase64.ConvertBase64ToImage();*/
            //image.Save($"{AppDomain.CurrentDomain.BaseDirectory}13.jpg", ImageFormat.Jpeg);

            //return;
            UploadResponse uploadResponse;
            do {
                var (key, value) = await _dataUploader.SetParameters(new WeciMexicoDvApiParam {
                    Url = "https://us-central1-ivoy-warehouse.cloudfunctions.net/weighing-machine",
                    TimeOut = 100000
                });
                uploadResponse = await _dataUploader.UploadData("SM12034AYMBX1T4YITRUCK0", 0.1, 2.2, 4.5,
                   0.1, image: fromFile, token: stoppingToken);
                //!string.IsNullOrEmpty(uploadResponse.ExceptionMsg)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            } while (!uploadResponse.IsSuccess);
            await File.AppendAllTextAsync($"{AppDomain.CurrentDomain.BaseDirectory}image.txt", uploadResponse.RequestContent, stoppingToken);
            Console.WriteLine(uploadResponse);
        }
    }
}