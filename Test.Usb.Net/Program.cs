using NLog;
using Usb.Net;
using Device.Net;
using Usb.Net.Windows;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

internal class Program {

    private static async Task Main(string[] args) {
        Console.WriteLine("Hello, World!");

        // 配置 NLog
        LogManager.LoadConfiguration("nlog.config");

        // 配置 LoggerFactory
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddNLog(); // 使用 NLog
            builder.SetMinimumLevel(LogLevel.Trace); // 设置最小日志级别
        });

        var logger = loggerFactory.CreateLogger("Program");
        logger.LogInformation("Logger initialized successfully.");
        logger.LogError("This is an error message.");
        var usbFactory =
            new FilterDeviceDefinition()
                .CreateWindowsUsbDeviceFactory(loggerFactory);
        // 获取所有可用的设备
        var devices = await usbFactory.GetConnectedDeviceDefinitionsAsync();

        foreach (var deviceDefinition in devices) {
            Console.WriteLine($"Found Device: Vendor ID={deviceDefinition.VendorId:X4}, Product ID={deviceDefinition.ProductId:X4}, Product ID={deviceDefinition.ProductName}, ReadBufferSize={deviceDefinition.ReadBufferSize},deviceID={deviceDefinition.DeviceId}");
        }

        var devId = "\\\\?\\hid#vid_04f2&pid_1026&mi_00#7&31622f9a&2&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}\\kbd";
        var device =
            new UsbDevice(
                devId, new WindowsUsbInterfaceManager(devId));

        // 打开设备连接
        await device.InitializeAsync();

        // 监测设备输入
        await MonitorDeviceInput(device);

        /*var definition = devices?.ToList()?.FirstOrDefault(f => f.ProductName.Contains("Wireless Device") && f.ReadBufferSize == 9);
        if (definition is not null) {
            var device = await usbFactory.GetDeviceAsync(definition);

            // 打开设备连接
            await device.InitializeAsync();

            // 监测设备输入
            await MonitorDeviceInput(device);
        }*/
    }

    private static async Task MonitorDeviceInput(IDevice device) {
        Console.WriteLine("Start monitoring device input...");

        while (true) {
            // 读取数据
            var readData = await device.ReadAsync();
            if (readData.Data.Length > 0) {
                // 处理读取的数据
                Console.WriteLine($"Data Received: {BitConverter.ToString(readData.Data)}");
            }

            // 选择性地添加延迟，以防止高 CPU 使用率
            await Task.Delay(100);
        }
    }
}