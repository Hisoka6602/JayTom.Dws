using HidSharp;
using System.Text;

internal class Program {

    private static void Main(string[] args) {
        var deviceList = DeviceList.Local;
        /*var hidDevices = deviceList.GetHidDevices().Where(w => w.GetProductName().
            Contains("Keyboard", StringComparison.CurrentCultureIgnoreCase))?.ToList() ?? new List<HidDevice>();*/
        var hidDevices = deviceList.GetHidDevices()
            ?.Where(w => w.GetProductName().Contains("Wireless Device"))
            ?.ToList() ?? new List<HidDevice>();
        foreach (var device in hidDevices) {
            Console.WriteLine($"Device Found: {device.GetProductName()}");
            //Console.WriteLine($"Device Found: {device.DevicePath}");
            Console.WriteLine($"  VID: {device.VendorID}, PID: {device.ProductID},MaxInputReportLength:{device.GetMaxInputReportLength()}");
            //Console.WriteLine($"  Manufacturer: {device.Manufacturer}, Product: {device.ProductName}");

            Console.WriteLine($"-----------------------------");
        }
        Console.ReadLine();
        var hidDevice = hidDevices?.FirstOrDefault(f => f.GetMaxInputReportLength() == 9);
        if (hidDevice is not null) {
            using (var stream = hidDevice.Open()) {
                var maxInputReportLength = hidDevice.GetMaxInputReportLength();
                byte[] buffer = new byte[hidDevice.GetMaxInputReportLength()];

                Console.WriteLine("Start listening to scanner input...");
                while (true) {
                    try {
                        /*buffer = hidDevice.GetRawReportDescriptor();
                        int bytesRead = buffer.Length;*/
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0) {
                            string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            Console.WriteLine($"Data Received: {data}");
                        }
                    }
                    catch (Exception e) {
                    }

                    // Optionally, add a small delay to avoid high CPU usage in the loop
                    Thread.Sleep(1000);
                }
            }
        }

        Console.ReadLine();
        Console.ReadLine();
    }
}