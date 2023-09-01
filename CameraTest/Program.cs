using Newtonsoft.Json;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

internal class Program {
    private static SemaphoreSlim _takePhotoSlim = new(1);

    private static async Task Main(string[] args) {
        var securityCamera = new DaHuatechSecurityCamera();
        securityCamera.PhotoTaken += async delegate (object? sender, PhotoTakenEventArgs eventArgs) {
            if (eventArgs.Image is not null) {
                try {
                    await _takePhotoSlim.WaitAsync();
                    eventArgs.Image?.Save(
                        $"{System.IO.Directory.GetCurrentDirectory()}\\Image\\{eventArgs.Barcode}.{eventArgs.BarcodeTimestamp}.jpg");
                    //写文件
                    eventArgs.Image?.Dispose();
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
                finally {
                    _takePhotoSlim.Release();
                }
            }
        };
        securityCamera.CameraExceptionOccurred += delegate (object? sender, CameraExceptionEventArgs eventArgs) {
            Console.WriteLine(JsonConvert.SerializeObject(eventArgs.Exception));
        };
        var initialize = await securityCamera.Initialize(string.Empty);
        await securityCamera.Start(null);
        for (var i = 0; i < 10; i++) {
            await securityCamera.TakePhotoAsync($"No0000000000{i + 1}__", DateTimeOffset.Now.ToUnixTimeMilliseconds());
        }

        Console.WriteLine("Hello, World!");
        Console.ReadLine();
        GC.Collect();
        Console.ReadLine();
    }
}