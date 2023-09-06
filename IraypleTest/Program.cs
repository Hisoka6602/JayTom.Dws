using Newtonsoft.Json;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Cameras.SmartCamera.Irayple;

internal class Program {

    private static async Task Main(string[] args) {
        var iraypleSmartCamera = new DaHuaSmartCamera();
        iraypleSmartCamera.CameraExceptionOccurred += delegate (object? sender, CameraExceptionEventArgs eventArgs) {
            Console.WriteLine($"异常:{JsonConvert.SerializeObject(eventArgs.Exception)}");
        };
        var enumerateCameras = await iraypleSmartCamera.EnumerateCameras();

        var (key, value) = await iraypleSmartCamera.Initialize(enumerateCameras?.FirstOrDefault() ?? new CameraInfo() {
            IpAddress = "192.168.31.63"
        });
        var (b, s) = await iraypleSmartCamera.Start(enumerateCameras?.FirstOrDefault() ?? new CameraInfo() {
            IpAddress = "192.168.31.63"
        });
        Console.ReadLine();
        Console.WriteLine("Hello, World!");
    }
}