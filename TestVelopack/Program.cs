using Velopack;

internal class Program {

    private static async Task Main(string[] args) {
        Console.WriteLine("Hello, World!");
        VelopackApp.Build().Run();

        //await UpdateMyApp();
        await Task.Yield();
        Console.ReadLine();
    }

    private static async Task UpdateMyApp() {
        var mgr = new UpdateManager("https://the.place/you-host/updates");

        // check for new version
        var newVersion = await mgr.CheckForUpdatesAsync();
        if (newVersion == null)
            return; // no update available

        // download new version
        await mgr.DownloadUpdatesAsync(newVersion);

        // install new version and restart app
        mgr.ApplyUpdatesAndRestart(newVersion);
    }
}