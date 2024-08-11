using NLog;
using Device.Net;
using Hid.Net.Windows;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

internal class Program {

    private static async Task Main(string[] args) {
        Console.WriteLine("Hello, World!");
        // 配置 NLog
        LogManager.LoadConfiguration("Nlog.config");

        // 配置 LoggerFactory
        var loggerFactory = LoggerFactory.Create(builder => {
            builder.AddNLog(); // 使用 NLog
            builder.SetMinimumLevel(LogLevel.Trace); // 设置最小日志级别
        });

        var logger = loggerFactory.CreateLogger("Program");
        logger.LogInformation("Logger initialized successfully.");
        logger.LogError("This is an error message.");
        // 创建一个设备工厂
        var deviceFactory = new FilterDeviceDefinition()
            .CreateWindowsHidDeviceFactory(loggerFactory);

        // 获取所有可用的设备
        var devices = await deviceFactory.GetConnectedDeviceDefinitionsAsync();

        foreach (var deviceDefinition in devices) {
            Console.WriteLine($"Found Device: Vendor ID={deviceDefinition.VendorId:X4}, Product ID={deviceDefinition.ProductId:X4}, ProductName={deviceDefinition.ProductName}, ReadBufferSize={deviceDefinition.ReadBufferSize},UsagePage={deviceDefinition.UsagePage:X4},Usage={deviceDefinition.Usage:X4}");
        }

        var definition = devices?.ToList()?.FirstOrDefault(f => f.ProductName.Contains("HIDI2C Device") && f.ReadBufferSize == 9);
        if (definition is not null) {
            var device = await deviceFactory.GetDeviceAsync(definition).ConfigureAwait(false);

            // 打开设备连接
            await device.InitializeAsync().ConfigureAwait(false);

            // 监测设备输入
            await MonitorDeviceInput(device);
        }
        /*// 初始化设备
        var device = await deviceFactory.GetDeviceAsync(deviceDefinition);

        // 打开设备连接
        await device.InitializeAsync();

        // 监测设备输入
        await MonitorDeviceInput(device);*/
        Console.ReadLine();
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