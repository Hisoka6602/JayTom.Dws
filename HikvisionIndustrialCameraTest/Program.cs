using Newtonsoft.Json;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;

internal class Program {

    private static async Task Main(string[] args) {
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
        var camera = new HikvisionIndustrialCamera();
        camera.BarcodeRead += async delegate (object? sender, BarcodeReadEventArgs eventArgs) {
            await Task.Delay(500);

            Console.WriteLine($"获取到条码:{eventArgs.Barcode}");
        };
        camera.CameraExceptionOccurred += delegate (object? sender, CameraExceptionEventArgs eventArgs) {
            Console.WriteLine($"相机异常:{eventArgs?.Exception?.Message}");
        };
        var infos = camera.EnumerateCameras();
        if (infos?.Any() == true) {
            foreach (var cameraInfo in infos) {
                Console.WriteLine(JsonConvert.SerializeObject(cameraInfo));
                Console.WriteLine("-------------------------------");
            }
        }

        Console.WriteLine("请输入需要连接的Id");
        var line = Console.ReadLine();
        int.TryParse(line, out var id);
        var (key, value) = await camera.Initialize(infos?[id]);
        await camera.Start(string.Empty);
        Console.ReadLine();
    }

    private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e) {
        // 处理未处理的异常
        Exception exception = e.ExceptionObject as Exception;
        Console.WriteLine("Unhandled Exception: " + exception?.Message);
        Console.ReadLine();
    }
}