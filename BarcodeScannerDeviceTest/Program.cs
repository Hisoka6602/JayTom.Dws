using HidSharp;
using LibUsbDotNet;
using Newtonsoft.Json;
using HidSharp.Utility;
using LibUsbDotNet.Main;
using System.Diagnostics;
using JayTom.Dws.Plugin.Device.BarcodeScannerDevice;

internal class Program {

    private static async Task Main(string[] args) {
        Console.WriteLine("Hello, World!");
        // Enumerate all USB devices

        var device = UsbDevice.AllDevices.ToList().FirstOrDefault();

        /*foreach (var device in devices) {
            Console.WriteLine($"Device: {device.Device.Info.ProductString},{device.Device.Info.Descriptor.ProductID:X4}, {device.Device.Info.Descriptor.VendorID:X4}");
        }*/
        UsbDevice.Exit();
        Console.WriteLine(device.Pid);
        Console.WriteLine(device.Vid);
        Console.WriteLine($"VID: {device.Vid:X4}, PID: {device.Pid:X4},MaxInputReportLength:{device.Rev}");
        var usbDevice = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(device.Vid, device.Pid));

        new UsbDeviceListener().StartListening(usbDevice);
        Console.ReadLine();

        Console.ReadLine();
        Console.ReadLine();
        Console.ReadLine();
        Console.ReadLine();
    }
}

public class UsbDeviceListener {
    private UsbDevice _device;
    private UsbEndpointReader _endpointReader;
    private Thread _readThread;
    private bool _keepReading;

    public void StartListening(UsbDevice device) {
        _device = device;
        // Assume endpoint 0x81 is the IN endpoint for data reading
        _endpointReader = _device.OpenEndpointReader(ReadEndpointID.Ep01);
        Console.WriteLine($"_endpointReader.ReadBufferSize:{_endpointReader.ReadBufferSize}");
        _keepReading = true;

        _readThread = new Thread(ReadData);
        _readThread.Start();

        Console.WriteLine("Listening for data... Press Enter to exit.");
        Console.ReadLine();

        StopListening();
    }

    private void ReadData() {
        while (_keepReading && _device.IsOpen) {
            try {
                int bytesRead;
                var buffer = new byte[64];  // 假设端点最大包大小为64字节，调整大小以适应你的设备
                ErrorCode ec = _endpointReader.Read(buffer, 200, out bytesRead);  // 增加超时时间

                if (ec == ErrorCode.Success && bytesRead > 0) {
                    Console.WriteLine("Data received: " + BitConverter.ToString(buffer, 0, bytesRead).Replace("-", " "));
                }
                else if (ec != ErrorCode.Success) {
                    // Console.WriteLine("Error reading data: " + ec);
                }
            }
            catch (Exception ex) {
                // Console.WriteLine("Exception: " + ex.Message);
            }
        }
    }

    private void StopListening() {
        _keepReading = false;
        _readThread?.Join();

        if (_endpointReader != null) {
            _endpointReader.Dispose();
            _endpointReader = null;
        }

        if (_device != null) {
            _device.Close();
            _device = null;
        }
    }
}