using OpenCvSharp;

using OpenCvSharp;

using LibUsbDotNet;
using Newtonsoft.Json;
using LibUsbDotNet.Main;
using System.Management;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

internal class Program {

    private static void Main(string[] args) {
        // 枚举并获取所有的摄像头设备
        /*
        var devices = Cv2.GetBuildInformation()
            .Split('\n')
            .Where(line => line.Contains("video input"))
            .Select(line => line.Trim());
        string[] strings = Cv2.GetBuildInformation()
            .Split('\n');
        var buildInformation = Cv2.GetBuildInformation();
        Console.WriteLine("Detected Video Input Devices:");

        foreach (var device in devices) {
            Console.WriteLine(device);
        }
        */

        /*
        for (int i = 0; i < 10; i++) {
            using (var capture = new VideoCapture(i)) {
                if (capture.IsOpened()) {
                    var deviceName = capture.Get( CaptureProperty.);
                    Console.WriteLine($"Device {i}: {deviceName}");
                    Console.WriteLine($"Device {i}: {deviceName}");
                }
            }
        }*/
        var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Camera'");
        var devices = searcher.Get();

        Console.WriteLine("Detected Video Input Devices:");

        foreach (var device in devices) {
            Console.WriteLine("-------------------------------");
            Console.WriteLine($"相机名称:{device["Caption"]?.ToString()}");
            Console.WriteLine($"相机GUID:{device["ClassGuid"]?.ToString()}");
            Console.WriteLine($"相机唯一标识:{device["DeviceID"]?.ToString()}");
            Console.WriteLine($"设备唯一标识:{device["PNPDeviceID"]?.ToString()}");
            Console.WriteLine($"硬件ID:{device["HardwareID"]?.ToString()}");
            Console.WriteLine($"设备类别:{device["PNPClass"]?.ToString()}");
            Console.WriteLine($"设备状态:{device["Status"]?.ToString()}");
            /*var deviceName = device["Caption"]?.ToString();
            // Console.WriteLine(deviceName);
            var serializeObject = JsonConvert.SerializeObject(device);*/
        }

        Console.ReadLine();
    }
}