using System;
using System.Linq;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Tcp.TcpServer {

    public class TouchSocketTcpServer : ITcpCommServer {
        private TcpService? _tcpService;
        public ConnectionStatus ConnectionStatus { get; private set; } = ConnectionStatus.Disconnected;

        public event EventHandler<string>? ConnectionException;

        public event EventHandler<Exception>? Exception;

        public event EventHandler<string>? Disconnected;

        public event EventHandler<CommunicationInfo>? Communication;

        public event EventHandler<string>? Connected;

        public event EventHandler<Exception>? SendError;

        public async Task<bool> Connect(string ipAddress, int port, int timeOut = 1000, FormatType dataType = FormatType.Ascii, CancellationToken token = default) {
            var parameter = SetParameter(new TcpConnectParam() {
                Address = ipAddress,
                Port = port,
                DataFormatType = dataType
            });
            if (parameter) {
                return await Connect(dataType, token);
            }

            return false;
        }

        public async Task<bool> Connect(FormatType dataType = FormatType.Ascii, CancellationToken token = default) {
            await Task.Yield();
            try {
                if (_tcpService is null) {
                    _tcpService = new TcpService();
                    _tcpService.Received += delegate (SocketClient client, ByteBlock block, IRequestInfo info) {
                        try {
                            var msg = Encoding.Default.GetString(block.Buffer, 0, block.Len);
                            OnCommunication(new CommunicationInfo() {
                                Content = dataType == FormatType.Ascii ? msg : BitConverter.ToString(RemoveTrailingZeros(block.Buffer)).Replace("-", " "),
                                Time = DateTime.Now,
                                Type = CommunicationType.Receive
                            });
                        }
                        catch (Exception e) {
                            NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                        }
                    };
                    _tcpService.Connected += delegate (SocketClient client, TouchSocketEventArgs args) {
                        OnConnected($"{client.ID}-{client.IP}:{client.Port} Connected");
                    };
                    _tcpService.Disconnected += delegate (SocketClient client, DisconnectEventArgs args) {
                        OnDisconnected($"{client.ID}-{client.IP}:{client.Port} Disconnected");
                    };
                }

                _tcpService.Start();

                return true;
            }
            catch (Exception e) {
                OnException(e);
                return false;
            }
            finally {
                ConnectionStatus = _tcpService?.ServerState == ServerState.Running ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
            }
        }

        public async Task<bool> Reconnect(int count, CancellationToken token = default) {
            for (var i = 0; i < count; i++) {
                var connect = await Connect(token: token);
                if (connect) {
                    return true;
                }
            }
            return false;
        }

        public bool SetParameter(object par) {
            if (par is TcpConnectParam tcpConnect) {
                try {
                    IpAddress = tcpConnect?.Address ?? string.Empty;
                    Port = tcpConnect?.Port ?? 0;
                    if (_tcpService is null) {
                        _tcpService = new TcpService();
                        _tcpService.Received += delegate (SocketClient client, ByteBlock block, IRequestInfo info) {
                            try {
                                var msg = Encoding.Default.GetString(block.Buffer, 0, block.Len);
                                OnCommunication(new CommunicationInfo() {
                                    Content = tcpConnect.DataFormatType == FormatType.Ascii ? msg : BitConverter.ToString(RemoveTrailingZeros(block.Buffer)).Replace("-", " "),
                                    Time = DateTime.Now,
                                    Type = CommunicationType.Receive
                                });
                            }
                            catch (Exception e) {
                                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                            }
                        };
                        _tcpService.Connected += delegate (SocketClient client, TouchSocketEventArgs args) {
                            OnConnected($"{client.ID}-{client.IP}:{client.Port} Connected");
                        };
                        _tcpService.Disconnected += delegate (SocketClient client, DisconnectEventArgs args) {
                            OnDisconnected($"{client.ID}-{client.IP}:{client.Port} Disconnected");
                        };
                    }

                    var listenIpHosts = new TouchSocketConfig().SetListenIPHosts(new IPHost[] { new($"{tcpConnect?.Address}:{tcpConnect?.Port}") });
                    _tcpService?.Setup(listenIpHosts);
                    return true;
                }
                catch (Exception e) {
                    OnException(e);
                    return false;
                }
            }

            return false;
        }

        public async Task<bool> SendMessage(string message, CancellationToken token = default) {
            try {
                if (ConnectionStatus == ConnectionStatus.Connected) {
                    //var bytes = Encoding.UTF8.GetBytes(message);
                    var clients = _tcpService?.SocketClients?.GetClients()?.ToList();
                    if (clients?.Any() == true) {
                        foreach (var socketClient in clients) {
                            await _tcpService.SendAsync(socketClient.ID, message);
                            OnCommunication(new CommunicationInfo() {
                                Content = message,
                                Time = DateTime.Now,
                                Type = CommunicationType.Send
                            });
                        }
                    }

                    return true;
                }
            }
            catch (Exception e) {
                OnSendError(e);
                return false;
            }
            return false;
        }

        public async Task<bool> SendMessage(byte[] message, CancellationToken token = default) {
            try {
                if (ConnectionStatus == ConnectionStatus.Connected) {
                    //var bytes = Encoding.UTF8.GetBytes(message);
                    var clients = _tcpService?.SocketClients?.GetClients()?.ToList();
                    if (clients?.Any() == true) {
                        foreach (var socketClient in clients) {
                            await _tcpService.SendAsync(socketClient.ID, message);
                            OnCommunication(new CommunicationInfo() {
                                Content = BitConverter.ToString(message).Replace("-", ", "),
                                Time = DateTime.Now,
                                Type = CommunicationType.Send
                            });
                        }
                    }

                    return true;
                }
            }
            catch (Exception e) {
                OnSendError(e);
                return false;
            }
            return false;
        }

        public void Close() {
            try {
                _tcpService?.Stop();
                _tcpService?.Dispose();
                ConnectionStatus = _tcpService?.ServerState == ServerState.Running ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
                _tcpService = null;
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        public string IpAddress { get; private set; } = string.Empty;
        public int Port { get; private set; } = 0;

        public event EventHandler<string>? ClientConnected;

        public event EventHandler<string>? ClientDisconnected;

        public async Task<bool> SendMessage(string ip, string message, CancellationToken token = default) {
            try {
                if (ConnectionStatus == ConnectionStatus.Connected) {
                    //var bytes = Encoding.UTF8.GetBytes(message);
                    var clients = _tcpService?.SocketClients?.GetClients()?.ToList();
                    if (clients?.Any() == true) {
                        var client = clients?.FirstOrDefault(f => f.IP.Equals(ip));
                        if (client is not null) {
                            await _tcpService.SendAsync(client.ID, message);
                            OnCommunication(new CommunicationInfo() {
                                Content = message,
                                Time = DateTime.Now,
                                Type = CommunicationType.Send
                            });
                        }
                        return true;
                    }
                }
            }
            catch (Exception e) {
                OnException(e);
                return false;
            }
            return false;
        }

        public async Task<bool> SendMessage(string ip, byte[] message, CancellationToken token = default) {
            try {
                if (ConnectionStatus == ConnectionStatus.Connected) {
                    //var bytes = Encoding.UTF8.GetBytes(message);
                    var clients = _tcpService?.SocketClients?.GetClients()?.ToList();
                    if (clients?.Any() == true) {
                        var client = clients?.FirstOrDefault(f => f.IP.Equals(ip));
                        if (client is not null) {
                            await _tcpService.SendAsync(client.ID, message);
                            OnCommunication(new CommunicationInfo() {
                                Content = BitConverter.ToString(message).Replace("-", ", "),
                                Time = DateTime.Now,
                                Type = CommunicationType.Send
                            });
                            return true;
                        }
                    }
                }
            }
            catch (Exception e) {
                OnException(e);
                return false;
            }
            return false;
        }

        public async Task<List<string>?> GetClientsIp() {
            await Task.Yield();
            return _tcpService?.SocketClients?.GetClients()?.Select(s => s.IP)?.ToList();
        }

        protected virtual async void OnConnectionException(string e) {
            await Task.Yield();
            ConnectionException?.Invoke(this, e);
        }

        protected virtual async void OnException(Exception e) {
            await Task.Yield();
            Exception?.Invoke(this, e);
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

        protected virtual async void OnSendError(Exception e) {
            await Task.Yield();
            SendError?.Invoke(this, e);
        }

        public byte[] RemoveTrailingZeros(byte[] input) {
            int lastIndex = Array.FindLastIndex(input, b => b != 0x00);
            byte[] result = new byte[lastIndex + 1];
            Array.Copy(input, result, lastIndex + 1);
            return result;
        }
    }
}