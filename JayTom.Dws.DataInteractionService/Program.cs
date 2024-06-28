using JayTom.Dws.DataInteractionService;

internal class Program {
    private static void Main(string[] args) {
        IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => {
        services.AddHostedService<Worker>();
    })
    .Build();

        host.Run();
    }
}