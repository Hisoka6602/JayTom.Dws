using Newtonsoft.Json;
using JayTom.Dws.Plugin.Device.BarcodeScannerDevice;

internal class Program {

    private static async Task Main(string[] args) {
        Console.WriteLine("Hello, World!");

        var barcodeScannerDevice = new BarcodeScannerDevice();
        var listHidDevices = await barcodeScannerDevice.GetListHidDevices();
        /*foreach (var hidDevice in listHidDevices) {
            Console.WriteLine(JsonConvert.SerializeObject(hidDevice, Formatting.Indented));
        }*/
        var hidDevice = listHidDevices.FirstOrDefault(f => f.GetProductName().Contains("HID POS"));
        if (hidDevice is not null) {
            await barcodeScannerDevice.StartListening(hidDevice, Console.WriteLine);
        }

        Console.ReadLine();
        Console.ReadLine();
        Console.ReadLine();
        Console.ReadLine();
    }
}