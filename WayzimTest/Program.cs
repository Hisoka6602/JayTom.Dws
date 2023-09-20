using JayTom.Dws.Camera.Cameras.SmartCamera.Wayzim;

internal class Program {

    private static async Task Main(string[] args) {
        var wayzimSmartCamera = new WayzimSmartCamera();
        wayzimSmartCamera.Initialize(null);
        await wayzimSmartCamera.EnumerateCameras();
        Console.WriteLine("Hello, World!");
        Console.ReadLine();
    }
}