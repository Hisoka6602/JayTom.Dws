using Newtonsoft.Json;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;

internal class Program {
    private static SemaphoreSlim _saveSlim = new(1);

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
        camera.RealtimeImage += async delegate (object? sender, RealtimeImageEventArgs eventArgs) {
            //存图到本地
            try {
                await _saveSlim.WaitAsync();
                eventArgs.ThumbImage?.Save($"{AppDomain.CurrentDomain.BaseDirectory}\\img\\{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.jpg");
                Console.WriteLine("已保存图片到本地");
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            finally {
                _saveSlim.Release();
            }
        };
        camera.CameraInitialized += delegate (object? sender, CameraInitializedEventArgs eventArgs) {
            if (!Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}\\img")) {
                Directory.CreateDirectory($"{AppDomain.CurrentDomain.BaseDirectory}\\img");
            }

            camera.StartRealTimeImage();
        };
        var infos = await camera.EnumerateCameras();
        if (infos?.Any() == true) {
            foreach (var cameraInfo in infos) {
                Console.WriteLine(JsonConvert.SerializeObject(cameraInfo, Formatting.Indented));
                Console.WriteLine("-------------------------------");
            }
        }

        Console.WriteLine("请输入需要连接的Id");
        var line = Console.ReadLine();
        int.TryParse(line, out var id);
        var (key, value) = await camera.Initialize(infos?[id]);
        Console.WriteLine(value);
        if (!key) {
            Console.WriteLine(value);
            return;
        }
        var (b, s) = await camera.Start(string.Empty);
        Console.WriteLine(s);
        Console.ReadLine();
        Console.ReadLine();
    }

    private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e) {
        // 处理未处理的异常
        Exception exception = e.ExceptionObject as Exception;
        Console.WriteLine("Unhandled Exception: " + exception?.Message);
        Console.ReadLine();
    }
}