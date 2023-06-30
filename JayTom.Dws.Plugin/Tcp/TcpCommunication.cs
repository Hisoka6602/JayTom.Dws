using NLog;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Sockets;

namespace JayTom.Dws.Plugin.Tcp {

    public class TcpCommunication : ITcpCommunication {
        private TcpService? _tcpService;

        public event EventHandler<string>? ConnectionException;

        public event EventHandler<string>? Disconnected;

        public event EventHandler<string>? Connected;

        public event EventHandler<CommunicationInfo>? Communication;

        public bool Connect() {
            _tcpService ??= new TcpService();
            try {
                _tcpService.Received += delegate (SocketClient client, ByteBlock block, IRequestInfo info) {
                    var msg = Encoding.Default.GetString(block.Buffer, 0, block.Len);
                    OnCommunication(new CommunicationInfo() {
                        Content = msg,
                        Time = DateTime.Now,
                        Type = CommunicationType.Receive
                    });
                };
                _tcpService.Connected += delegate (SocketClient client, TouchSocketEventArgs args) {
                    OnConnected($"{client.ID}-{client.IP}:{client.Port} 连接成功");
                };
                _tcpService.Disconnected += delegate (SocketClient client, DisconnectEventArgs args) {
                    OnDisconnected($"{client.ID}-{client.IP}:{client.Port} 断开连接");
                };
                _tcpService.Start();
                return true;
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"{e}");
                return false;
            }
        }

        public bool Reconnect(int count) {
            for (var i = 0; i < count; i++) {
                var connect = Connect();
                if (connect) {
                    return true;
                }
            }
            return false;
        }

        public bool SetParameter(object par) {
            if (par is TcpConnectParam tcpConnectt) {
                try {
                    _tcpService ??= new TcpService();

                    var listenIpHosts = new TouchSocketConfig().SetListenIPHosts(new IPHost[] { new($"{tcpConnectt.Address}:{tcpConnectt.Port}") });
                    _tcpService?.Setup(listenIpHosts);
                    return true;
                }
                catch (Exception e) {
                    LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"{e}");
                    return false;
                }
            }

            return false;
        }

        public void Close() {
            _tcpService?.Stop();
            _tcpService?.Dispose();
        }

        protected virtual async void OnCommunication(CommunicationInfo e) {
            await Task.Yield();
            Communication?.Invoke(this, e);
        }

        protected virtual async void OnConnected(string e) {
            await Task.Yield();
            LogManager.GetCurrentClassLogger().Log(LogLevel.Info, e);
            Connected?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(string e) {
            await Task.Yield();
            LogManager.GetCurrentClassLogger().Log(LogLevel.Info, e);
            Disconnected?.Invoke(this, e);
        }
    }

    public class TcpConnectParam {

        /// <summary>
        /// 地址
        /// </summary>
        public string? Address { get; set; } = "127.0.0.1";

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }
    }
}