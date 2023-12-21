using Newtonsoft.Json;
using JayTom.Dws.Plugin.Speech;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

internal class Program {

    private static async Task Main(string[] args) {
        var daHuatechSecurityCamera = new DaHuatechSecurityCamera();
        var enumerateCameras = await daHuatechSecurityCamera.EnumerateCameras();
        enumerateCameras?.ForEach(f => {
            Console.WriteLine(JsonConvert.SerializeObject(f));
            Console.WriteLine("--------------------");
        });

        // var devices = await BaseDaHuatech.EnumDevices();

        var baseDaHuatech = BaseDaHuatech.CreateInstance();

        /*
        devices?.ForEach(f => {
            Console.WriteLine(JsonConvert.SerializeObject(f));
            Console.WriteLine("--------------------");
        });*/
        var (b, s) = await baseDaHuatech.LogIn("9J05146PAZ13278", "admin", "a12345678");
        Console.WriteLine($"{s}");
        var (key, value) = await baseDaHuatech.StartRemotePlayback("9J05146PAZ13278", 1, DateTime.Today,
            DateTime.Today.AddDays(1).AddSeconds(-1), 10);
        Console.WriteLine($"{key}---{value}");
        Console.ReadLine();
    }
}