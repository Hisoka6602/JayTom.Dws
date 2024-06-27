using Newtonsoft.Json;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using JayTom.Dws.CrossCutting.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using JayTom.Dws.Domain.Entities.SystemEntities;

namespace SignalRTest;

internal class Program {

    private static void Main(string[] args) {
        var host = CreateHostBuilder(args).Build();

        using (var serviceScope = host.Services.CreateScope()) {
            var services = serviceScope.ServiceProvider;
        }

        // 如果需要后续执行其他逻辑，可以继续添加到这里

        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) => {
                services.AddSingleton<IBaseClientMessageHub, BaseClientMessageHub>();
                services.AddHostedService<Worker>();
            });

    public class Worker : BackgroundService {
        private readonly IBaseClientMessageHub _baseClientMessageHub;

        public Worker(IBaseClientMessageHub baseClientMessageHub) {
            _baseClientMessageHub = baseClientMessageHub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await _baseClientMessageHub.StartAsync("http://100.248.248.101:8080/Message", a => {
                a.On<object>("SystemInfo", async data => {
                    var computerInfo = JsonConvert.DeserializeObject<ComputerInfoModel>(data.ToString() ?? string.Empty);

                    // 现在您可以使用 computerInfo 对象进行后续处理
                    Console.WriteLine($"Received computer info: {computerInfo}");

                    Console.WriteLine(data);
                });
            }, "aa");
            return;
        }
    }
}