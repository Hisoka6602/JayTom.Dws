using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Data;
using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.Plugin.Tcp.TcpServer {

    public class TouchSocketTcpServer : ITcpCommServer {
        private TcpService? _tcpService;
        public FormatType FormatType { get; set; }
        public ConnectionStatus ConnectionStatus { get; private set; } = ConnectionStatus.Disconnected;

        public event EventHandler<string>? ConnectionException;

        public event EventHandler<Exception>? Exception;

        public event EventHandler<string>? Disconnected;

        public event EventHandler<CommunicationInfo>? Communication;

        public event EventHandler<string>? Connected;

        public event EventHandler<Exception>? SendError;

        private readonly SemaphoreSlim _sendSlim = new(1, 1);

        public async Task<bool> Connect(string ipAddress, int port, int timeOut = 1000, FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default) {
            DataLen = dataLen;
            FormatType = dataType;
            var parameter = SetParameter(new TcpConnectParam() {
                Address = ipAddress,
                Port = port,
                DataFormatType = dataType,
                DataLength = dataLen
            });
            if (parameter) {
                return await Connect(dataType, dataLen, token);
            }

            return false;
        }

        public async Task<bool> Connect(FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default) {
            try {
                DataLen = dataLen;
                FormatType = dataType;
                if (_tcpService is null) {
                    _tcpService = new TcpService();
                    _tcpService.Received += delegate (SocketClient client, ByteBlock block, IRequestInfo info) {
                        try {
                            var length = DataLen > 0 ? Math.Min(DataLen, block.Len) : block.Len;
                            var msg = Encoding.Default.GetString(block.Buffer, 0, length);
                            OnCommunication(new CommunicationInfo() {
                                Content = FormatType == FormatType.Ascii
                                    ? msg
                                    : Convert.ToHexString(block.Buffer.AsSpan(0, length)),
                                Time = DateTime.Now,
                                Type = CommunicationType.Receive
                            });
                            block.Clear();
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
                FormatType = tcpConnect.DataFormatType;
                DataLen = tcpConnect.DataLength;
                try {
                    IpAddress = tcpConnect.Address ?? string.Empty;
                    Port = tcpConnect.Port;
                    if (_tcpService is null) {
                        _tcpService = new TcpService();
                        _tcpService.Received += delegate (SocketClient client, ByteBlock block, IRequestInfo info) {
                            try {
                                var length = DataLen > 0 ? Math.Min(DataLen, block.Len) : block.Len;
                                var msg = Encoding.Default.GetString(block.Buffer, 0, length);
                                OnCommunication(new CommunicationInfo() {
                                    Content = FormatType == FormatType.Ascii
                                        ? msg
                                        : Convert.ToHexString(block.Buffer.AsSpan(0, length)),
                                    Time = DateTime.Now,
                                    Type = CommunicationType.Receive
                                });
                                block.Clear();
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

                    var listenIpHosts = new TouchSocketConfig().SetListenIPHosts(new IPHost[] { new($"{IPAddress.Any.ToString()}:{tcpConnect.Port}") })
                        .SetBufferLength(tcpConnect.DataLength);

                    //var listenIpHosts = new TouchSocketConfig().SetListenIPHosts(new IPHost[] { new($"{tcpConnect?.Address}:{tcpConnect?.Port}") });
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
            var lockTaken = false;
            try {
                await _sendSlim.WaitAsync(token);
                lockTaken = true;
                if (ConnectionStatus == ConnectionStatus.Connected) {
                    //var bytes = Encoding.UTF8.GetBytes(message);
                    var clients = _tcpService?.SocketClients?.GetClients()?.ToList();
                    if (clients?.Any() == true) {
                        foreach (var socketClient in clients) {
                            await _tcpService.SendAsync(socketClient.ID, message);
                        }
                        OnCommunication(new CommunicationInfo {
                            Content = message,
                            Time = DateTime.Now,
                            Type = CommunicationType.Send
                        });
                    }
                    return true;
                }
            }
            catch (Exception e) {
                OnSendError(e);
                return false;
            }
            finally {
                if (lockTaken) {
                    _sendSlim.Release();
                }
            }
            return false;
        }

        public async Task<bool> SendMessage(byte[] message, CancellationToken token = default) {
            var lockTaken = false;
            try {
                await _sendSlim.WaitAsync(token);
                lockTaken = true;
                if (ConnectionStatus == ConnectionStatus.Connected) {
                    //var bytes = Encoding.UTF8.GetBytes(message);
                    var clients = _tcpService?.SocketClients?.GetClients()?.ToList();
                    if (clients?.Any() == true) {
                        foreach (var socketClient in clients) {
                            await _tcpService.SendAsync(socketClient.ID, message);
                        }
                        OnCommunication(new CommunicationInfo {
                            Content = Convert.ToHexString(message),
                            Time = DateTime.Now,
                            Type = CommunicationType.Send
                        });
                    }

                    return true;
                }
            }
            catch (Exception e) {
                OnSendError(e);
                return false;
            }
            finally {
                if (lockTaken) {
                    _sendSlim.Release();
                }
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

        private int _dataLen = 0;

        public int DataLen {
            get => _dataLen;
            set => _dataLen = value;
        }

        public event EventHandler<string>? ClientConnected;

        public event EventHandler<string>? ClientDisconnected;

        public async Task<bool> SendMessage(string ip, string message, CancellationToken token = default) {
            var lockTaken = false;
            try {
                await _sendSlim.WaitAsync(token);
                lockTaken = true;
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
            finally {
                if (lockTaken) {
                    _sendSlim.Release();
                }
            }
            return false;
        }

        public async Task<bool> SendMessage(string ip, byte[] message, CancellationToken token = default) {
            var lockTaken = false;
            try {
                await _sendSlim.WaitAsync(token);
                lockTaken = true;
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
            finally {
                if (lockTaken) {
                    _sendSlim.Release();
                }
            }
            return false;
        }

        public Task<List<string>?> GetClientsIp() {
            return Task.FromResult(_tcpService?.SocketClients?.GetClients()?.Select(s => s.IP)?.ToList());
        }

        protected virtual void OnConnectionException(string e) {
            ConnectionException?.Invoke(this, e);
        }

        protected virtual void OnException(Exception e) {
            Exception?.Invoke(this, e);
        }

        protected virtual void OnCommunication(CommunicationInfo e) {
            Communication?.Invoke(this, e);
        }

        protected virtual void OnConnected(string e) {
            Connected?.Invoke(this, e);
        }

        protected virtual void OnDisconnected(string e) {
            Disconnected?.Invoke(this, e);
        }

        protected virtual void OnSendError(Exception e) {
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
