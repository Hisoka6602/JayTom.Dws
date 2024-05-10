using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Security.Policy;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols;

internal class Program {
    private static int _startNum = 0;
    private static int _endNum = 0;
    private static int _indexNum = 0;

    private static async Task Main(string[] args) {
        Console.WriteLine("开始启动...");

        var protocol = new WxkcCommunicationProtocol();
        var tcpOperations = new BaseTcpOperations(new TouchSocketTcpClient(), new TouchSocketTcpServer());
        tcpOperations.Communication += async (sender, info) => {
            if (info.Type == CommunicationType.Receive) {
                var deviceDecodeResult = protocol.DecodeData(info.Content);
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}:接收的内容:{info.Content}");
                if (deviceDecodeResult is not null && deviceDecodeResult.Type == FunctionType.CreatePackage) {
                    //发送
                    var encodeData = protocol.EncodeData(FunctionType.SendExit, null, _indexNum.ToString("X4"), new InstructionsAttach() {
                        Guid = Convert.ToInt32(deviceDecodeResult.Keyword)
                    });
                    await tcpOperations.SendMessage(encodeData);
                    if (_indexNum >= _endNum) {
                        _indexNum = _startNum;
                    }
                    else {
                        _indexNum++;
                    }
                }
            }
            else if (info.Type == CommunicationType.Send) {
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}:发送的内容:{info.Content}");
            }
        };
        //读配置
        try {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath($"{AppContext.BaseDirectory}")
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            _startNum = Convert.ToInt32(configuration["StartExitNum"]);
            _endNum = Convert.ToInt32(configuration["EndExitNum"]);
            //连接
            var connect = await tcpOperations.Connect(configuration["IpAddress"] ?? string.Empty, Convert.ToInt32(configuration["Port"]),
                ConnectionType.Client,
                1000, FormatType.Hex);
            Console.WriteLine(connect
                ? $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}:启动完成,按下回车结束测试..."
                : $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}:连接失败");
        }
        catch (Exception e) {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}:读取参数失败!");
        }

        //连接

        Console.ReadLine();
    }
}