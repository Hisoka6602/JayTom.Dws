using NLog;
using System;
using System.Linq;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Tcp {

    public class TcpCommunicationClient : ITcpCommunicationClient {
        private TcpClient? _tcpClient;

        public bool IsConnected {
            get => _isConnected;
            private set => _isConnected = value;
        }

        public event EventHandler<string>? ConnectionException;

        public event EventHandler<Exception>? Exception;

        public event EventHandler<string>? Connected;

        public event EventHandler<string>? Disconnected;

        public event EventHandler<CommunicationInfo>? Communication;

        private static bool _isConnected;

        public async Task<bool> Connect() {
            _tcpClient ??= new TcpClient();
            try {
                _tcpClient.Received += delegate (TcpClient client, ByteBlock block, IRequestInfo info) {
                    var msg = Encoding.Default.GetString(block.Buffer, 0, block.Len);
                    OnCommunication(new CommunicationInfo() {
                        Content = msg,
                        Time = DateTime.Now,
                        Type = CommunicationType.Receive
                    });
                };
                _tcpClient.Connected += delegate {
                    IsConnected = true;
                    OnConnected($"连接成功");
                };
                _tcpClient.Disconnected += delegate (ITcpClientBase client, DisconnectEventArgs args) {
                    IsConnected = false;
                    OnDisconnected($"断开连接");
                };
                var tcpClient = await _tcpClient.ConnectAsync(60 * 1000);
                return true;
            }
            catch (Exception e) {
                OnException(e);
                return false;
            }
        }

        public async Task<bool> Reconnect(int count) {
            for (var i = 0; i < count; i++) {
                await Connect();
                if (_isConnected) {
                    return true;
                }
            }
            return false;
        }

        public bool SetParameter(object par) {
            if (par is TcpConnectParam tcpConnect) {
                try {
                    _tcpClient ??= new TcpClient();

                    var touchSocketConfig = new TouchSocketConfig().SetRemoteIPHost(new IPHost($"{tcpConnect.Address}:{tcpConnect.Port}"))
                        .UsePlugin().SetBufferLength(1024 * 10).ConfigurePlugins(a => {
                            a.UseReconnection(20, true, 1000);
                        });

                    _tcpClient?.Setup(touchSocketConfig);
                    return true;
                }
                catch (Exception e) {
                    OnException(e);
                    return false;
                }
            }

            return false;
        }

        public async Task<bool> SendMessage(string message) {
            try {
                if (_isConnected) {
                    //var bytes = Encoding.UTF8.GetBytes(message);
                    await _tcpClient.SendAsync(message);
                    OnCommunication(new CommunicationInfo() {
                        Content = message,
                        Time = DateTime.Now,
                        Type = CommunicationType.Send
                    });
                    return true;
                }
            }
            catch (Exception e) {
                OnException(e);
                return false;
            }
            return false;
        }

        public async Task<bool> SendMessage(byte[] message) {
            try {
                if (_isConnected) {
                    await _tcpClient.SendAsync(message);
                    OnCommunication(new CommunicationInfo() {
                        Content = BitConverter.ToString(message).Replace("-", ", "),
                        Time = DateTime.Now,
                        Type = CommunicationType.Send
                    });
                    return true;
                }
            }
            catch (Exception e) {
                OnException(e);
                return false;
            }
            return false;
        }

        public void Close() {
            _tcpClient?.Close();
            //_tcpClient?.Dispose();
            IsConnected = false;
        }

        protected virtual async void OnCommunication(CommunicationInfo e) {
            await Task.Yield();
            Communication?.Invoke(this, e);
        }

        protected virtual async void OnConnected(string e) {
            await Task.Yield();
            Connected?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(string e) {
            await Task.Yield();
            Disconnected?.Invoke(this, e);
        }

        protected virtual async void OnException(Exception e) {
            await Task.Yield();
            LogManager.GetCurrentClassLogger().Log(LogLevel.Error, $"{e}");
            Exception?.Invoke(this, e);
        }
    }
}