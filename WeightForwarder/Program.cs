using System.IO.Ports;
using Microsoft.Extensions.Configuration;

internal class Program {
    private static readonly SemaphoreSlim _semaphore = new(1);
    private static SerialPort? toPort;

    private static void Main(string[] args) {
        string inputString = "格口abc123def456";

        // 使用LINQ和Char.IsDigit来获取字符串中的数字
        string numbersOnly = new string(inputString.Where(char.IsDigit).ToArray());

        Console.WriteLine("Original string: " + inputString);
        Console.WriteLine("Numbers only: " + numbersOnly);

        Console.WriteLine("读配置");
        try {
            var configuration = new ConfigurationBuilder()
                .SetBasePath($"{AppContext.BaseDirectory}")
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var empty = configuration["From:PortName"] ?? string.Empty;
            var fromPort = new SerialPort() {
                PortName = configuration["From:PortName"] ?? string.Empty,
                BaudRate = Convert.ToInt32(configuration["From:BaudRate"]),
                DataBits = Convert.ToInt32(configuration["From:DataBits"]),
                Parity = (Parity)Convert.ToInt32(configuration["From:Parity"]),
                StopBits = (StopBits)Convert.ToInt32(configuration["From:StopBits"]),
            };
            if (fromPort is not null) {
                //定义接收事件
                fromPort.DataReceived += FromPortOnDataReceived;
                fromPort.Open();
                Console.WriteLine($"{fromPort.PortName}连接{(fromPort.IsOpen ? "成功" : "失败")}");
            }
            else {
                Console.WriteLine($"{fromPort.PortName}连接失败");
            }
            toPort = new SerialPort() {
                PortName = configuration["To:PortName"] ?? string.Empty,
                BaudRate = Convert.ToInt32(configuration["To:BaudRate"]),
                DataBits = Convert.ToInt32(configuration["To:DataBits"]),
                Parity = (Parity)Convert.ToInt32(configuration["To:Parity"]),
                StopBits = (StopBits)Convert.ToInt32(configuration["To:StopBits"]),
            };
            if (toPort is not null) {
                //定义发送
                toPort.Open();
                Console.WriteLine($"{toPort.PortName}连接{(toPort.IsOpen ? "成功" : "失败")}");
            }
            else {
                Console.WriteLine($"{toPort.PortName}连接失败");
            }

            Console.ReadLine();
        }
        catch (Exception e) {
            Console.WriteLine($"读取配置失败:{e.Message}");
        }
    }

    private static async void FromPortOnDataReceived(object sender, SerialDataReceivedEventArgs e) {
        try {
            await _semaphore.WaitAsync();
            if (sender is System.IO.Ports.SerialPort { IsOpen: true, BytesToRead: > 0 } port) {
                var receivedData = port.ReadExisting();
                receivedData = receivedData.TrimEnd('\r', '\n');
                Console.WriteLine($"<-接收到的内容:{receivedData}");
                if (!string.IsNullOrEmpty(receivedData)) {
                    var charArray = receivedData.ToCharArray();
                    charArray[^1] = '9'; // 使用索引[^1]表示最后一个字符
                    receivedData = new string(charArray);
                }

                if (toPort?.IsOpen == true) {
                    toPort.WriteLine(receivedData);
                    Console.WriteLine($"->发送的内容:{receivedData}");
                }
            }
        }
        finally {
            _semaphore.Release();
        }
    }
}