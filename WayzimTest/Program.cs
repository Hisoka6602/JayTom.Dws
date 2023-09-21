using Newtonsoft.Json;
using JayTom.Dws.Camera.Cameras.SmartCamera.Wayzim;

internal class Program {

    private static async Task Main(string[] args) {
        /*var baseWayzim = BaseWayzim.CreateInstance();
        var wayzimDeviceInfos = await BaseWayzim.EnumDevices();*/
        var wayzimSmartCamera = new WayzimSmartCamera();
        var enumerateCameras = await wayzimSmartCamera.EnumerateCameras();

        var firstOrDefault = enumerateCameras?.FirstOrDefault(f => f.Name.Equals("t1"));
        if (firstOrDefault is not null) {
            await wayzimSmartCamera.Initialize(firstOrDefault);
            await wayzimSmartCamera.Start(null);
        }

        //Console.WriteLine(JsonConvert.SerializeObject(enumerateCameras, Formatting.Indented));
        Console.WriteLine("Hello, World!");
        Console.ReadLine();
    }
}