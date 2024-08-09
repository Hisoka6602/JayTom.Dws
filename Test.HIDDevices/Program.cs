using HidSharp;

internal class Program {

    private static void Main(string[] args) {
        var devices = HIDDevice.GetDevices();
        foreach (var device in devices) {
            Console.WriteLine($"Device: {device.ProductName}, {device.VendorId:X4}:{device.ProductId:X4}");
        }
    }
}