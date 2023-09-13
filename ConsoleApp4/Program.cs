using JayTom.Dws.Plugin.Speech;

internal class Program {

    private static async Task Main(string[] args) {
        var speech = new Speech();
        var filePath = Path.Combine(AppContext.BaseDirectory, "success.wav");
        var data = await File.ReadAllBytesAsync(filePath);
        var i = 1;
        while (true) {
            await speech.PlayCacheByteFile("success", data);
            Console.WriteLine($"播放:{i}");
            i++;
        }

        Console.WriteLine("Hello, World!");
    }
}