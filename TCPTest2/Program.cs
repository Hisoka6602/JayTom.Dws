using System.Net;
using System.Text;
using System.Data;
using TouchSocket.Core;
using TouchSocket.Sockets;
using JayTom.Dws.Plugin.Tcp;
using JayTom.Dws.Plugin.Tcp.TcpServer;

internal class Program {
    private static TcpService? _tcpService;

    private static Task Main(string[] args) {
        Console.WriteLine("Hello, World!");

        //new TouchSocketTcpServer()

        if (_tcpService is null) {
            _tcpService = new TcpService();
            var listenIpHosts = new TouchSocketConfig().SetListenIPHosts(new IPHost[]
                { new($"{"192.168.21.74"}:{2000}") });

            _tcpService.Setup(listenIpHosts);

            _tcpService.Received += async delegate (SocketClient client, ByteBlock block, IRequestInfo info) {
                await Task.Yield();
                try {
                    /*var msg = Encoding.Default.GetString(block.Buffer, 0, DataLen > 0 ? DataLen : block.Len);
                    OnCommunication(new CommunicationInfo() {
                        Content = dataType == FormatType.Ascii ? msg : BitConverter.ToString(block.Buffer.Take(DataLen > 0 ? DataLen : block.Len).ToArray()).Replace("-", " "),
                        Time = DateTime.Now,
                        Type = CommunicationType.Receive
                    });*/
                    if (block.Length >= 8) {
                        Console.WriteLine($"接收到的内容:{BitConverter.ToString(block.Buffer.Take(block.Len).ToArray()).Replace("-", " ")}");
                        await Task.Delay(5000);
                        await block.ClearAsync();
                    }

                    var bytes = new byte[] { 0xf9, 0x11, 0x00, 0x37, 0x00, 0x64, 0x01, 0x43 };
                    var clients = _tcpService?.SocketClients?.GetClients()?.ToList();
                    if (clients?.Any() == true) {
                        foreach (var socketClient in clients) {
                            await _tcpService?.SendAsync(socketClient.ID, bytes)!;
                        }
                        Console.WriteLine($"发送的内容:{BitConverter.ToString(bytes).Replace("-", " ")}");
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            };
            _tcpService.Connected += delegate (SocketClient client, TouchSocketEventArgs args) {
                Console.WriteLine("客户端连接!");
            };
            _tcpService.Disconnected += delegate (SocketClient client, DisconnectEventArgs args) {
                Console.WriteLine("客户端关闭!");
            };
        }

        _tcpService.Start();

        Console.ReadLine();
        return Task.CompletedTask;
    }
}