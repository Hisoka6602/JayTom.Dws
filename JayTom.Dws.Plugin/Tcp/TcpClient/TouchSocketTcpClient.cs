using System;
using System.Linq;
using System.Text;
using System.Data;
using TouchSocket.Core;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.Plugin.Tcp.TcpClient {
    public class TouchSocketTcpClient : ITcpCommClient {
        private TouchSocket.Sockets.TcpClient? _tcpClient;
        public string IpAddress { get; private set; } = string.Empty;
        public int Port { get; private set; } = 0;
        private int _dataLen = 0;
        private SemaphoreSlim _sendSlim = new(1);

        public int DataLen {
            get => _dataLen;
            set => _dataLen = value;
        }

        public FormatType FormatType { get; set; }
        public ConnectionStatus ConnectionStatus { get; private set; } = ConnectionStatus.Connected;

        public event EventHandler<string>? ConnectionException;

        public event EventHandler<Exception>? Exception;

        public event EventHandler<string>? Disconnected;

        public event EventHandler<CommunicationInfo>? Communication;

        public event EventHandler<string>? Connected;

        public event EventHandler<Exception>? SendError;

        public async Task<bool> Connect(string ipAddress, int port, int timeOut = 1000, FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default) {
            FormatType = dataType;
            var parameter = SetParameter(new TcpConnectParam() {
                Address = ipAddress,
                Port = port,
                DataFormatType = dataType,
                DataLength = dataLen
            });
            DataLen = dataLen;
            if (parameter) {
                return await Connect(dataType, dataLen, token);
            }

            return false;
        }

        public async Task<bool> Connect(FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default) {
            try {
                DataLen = dataLen;
                FormatType = dataType;
                if (_tcpClient is null) {
                    _tcpClient = new TouchSocket.Sockets.TcpClient();
                    _tcpClient.Received += delegate (TouchSocket.Sockets.TcpClient client, ByteBlock block, IRequestInfo info) {
                        try {
                            var msg = Encoding.Default.GetString(block.Buffer, 0, DataLen > 0 ? DataLen : block.Len);
                            OnCommunication(new CommunicationInfo() {
                                Content = dataType == FormatType.Ascii ? msg : BitConverter.ToString(block.Buffer.Take(DataLen > 0 ? DataLen : block.Len).ToArray()).Replace("-", " "),
                                Time = DateTime.Now,
                                Type = CommunicationType.Receive
                            });
                            block.Clear();
                        }
                        catch (Exception e) {
                            NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                        }
                    };
                    _tcpClient.Connected += delegate {
                        OnConnected($"IpAddress:{IpAddress},Port:{Port}");
                    };
                    _tcpClient.Disconnected += delegate (ITcpClientBase client, DisconnectEventArgs args) {
                        OnDisconnected($"IpAddress:{IpAddress},Port:{Port}");
                    };
                }

                var tcpClient = await _tcpClient.ConnectAsync(60 * 1000);
                if (tcpClient is not null &&
                    tcpClient.IP.Equals(IpAddress) &&
                    tcpClient.Port.Equals(Port)) {
                    return true;
                }
            }
            catch (Exception e) {
                OnException(e);
                return false;
            }
            return false;
        }

        public async Task<bool> Reconnect(int count, CancellationToken token = default) {
            if (count > 0) {
                for (var i = 0; i < count; i++) {
                    await Connect(token: token);
                    await Task.Delay(500, token);
                    if (ConnectionStatus == ConnectionStatus.Connected) {
                        return true;
                    }
                }

            }
            else {
                do {
                    await Connect(token: token);
                    await Task.Delay(500, token);
                    if (ConnectionStatus == ConnectionStatus.Connected) {
                        return true;
                    }
                } while (true);
            }
            return false;
        }

        public bool SetParameter(object par) {
            if (par is TcpConnectParam tcpConnect) {
                try {
                    FormatType = tcpConnect.DataFormatType;
                    IpAddress = tcpConnect.Address ?? string.Empty;
                    Port = tcpConnect.Port;
                    DataLen = tcpConnect.DataLength;
                    if (_tcpClient is null) {
                        _tcpClient = new TouchSocket.Sockets.TcpClient();
                        _tcpClient.Received += delegate (TouchSocket.Sockets.TcpClient client, ByteBlock block, IRequestInfo info) {
                            try {
                                var msg = Encoding.Default.GetString(block.Buffer, 0, DataLen > 0 ? DataLen : block.Len);
                                OnCommunication(new CommunicationInfo() {
                                    Content = tcpConnect.DataFormatType == FormatType.Ascii ? msg : BitConverter.ToString(block.Buffer.Take(DataLen > 0 ? DataLen : block.Len).ToArray()).Replace("-", " "),
                                    Time = DateTime.Now,
                                    Type = CommunicationType.Receive
                                });
                                block.Clear();
                            }
                            catch (Exception e) {
                                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                            }
                        };
                        _tcpClient.Connected += delegate {
                            OnConnected($"IpAddress:{IpAddress},Port:{Port}");
                        };
                        _tcpClient.Disconnected += delegate (ITcpClientBase client, DisconnectEventArgs args) {
                            OnDisconnected($"IpAddress:{IpAddress},Port:{Port}");
                        };
                    }
                    var touchSocketConfig = new TouchSocketConfig().SetRemoteIPHost(new IPHost($"{tcpConnect.Address}:{tcpConnect.Port}"))
                        .UsePlugin().SetBufferLength(tcpConnect.DataLength).ConfigurePlugins(a => {
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

        public async Task<bool> SendMessage(string message, CancellationToken token = default) {
            try {
                if (ConnectionStatus == ConnectionStatus.Connected) {
                    await _sendSlim.WaitAsync(token);
                    await _tcpClient.SendAsync(message);
                    OnCommunication(new CommunicationInfo() {
                        Content = message,
                        Time = DateTime.Now,
                        Type = CommunicationType.Send
                    });
                    await Task.Delay(5, token);
                    return true;
                }
                else {
                    OnException(
                        new Exception($"IpAddress:{IpAddress},Port:{Port},ConnectionStatus:{ConnectionStatus}"));
                }
            }
            catch (Exception e) {
                OnSendError(e);
                return false;
            }
            finally {
                _sendSlim.Release();
            }
            return false;
        }

        public async Task<bool> SendMessage(byte[] message, CancellationToken token = default) {
            try {
                if (ConnectionStatus == ConnectionStatus.Connected) {
                    await _sendSlim.WaitAsync(token);
                    await _tcpClient.SendAsync(message);
                    OnCommunication(new CommunicationInfo() {
                        Content = BitConverter.ToString(message).Replace("-", ", "),
                        Time = DateTime.Now,
                        Type = CommunicationType.Send
                    });
                    await Task.Delay(5, token);
                    return true;
                }
                else {
                    OnException(
                        new Exception($"IpAddress:{IpAddress},Port:{Port},ConnectionStatus:{ConnectionStatus}"));
                }
            }
            catch (Exception e) {
                OnSendError(e);
                return false;
            }
            finally {
                _sendSlim.Release();
            }
            return false;
        }

        public void Close() {
            _tcpClient?.Close();
            _tcpClient = null;
            ConnectionStatus = ConnectionStatus.Disconnected;
        }

        protected virtual async void OnConnectionException(string e) {
            await Task.Yield();
            ConnectionException?.Invoke(this, e);
        }

        protected virtual async void OnException(Exception e) {
            await Task.Yield();
            Exception?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(string e) {
            await Task.Yield();
            ConnectionStatus = ConnectionStatus.Disconnected;
            Disconnected?.Invoke(this, e);
        }

        protected virtual async void OnCommunication(CommunicationInfo e) {
            await Task.Yield();
            Communication?.Invoke(this, e);
        }

        protected virtual async void OnConnected(string e) {
            await Task.Yield();
            ConnectionStatus = ConnectionStatus.Connected;
            Connected?.Invoke(this, e);
        }

        protected virtual async void OnSendError(Exception e) {
            await Task.Yield();
            SendError?.Invoke(this, e);
        }

        public byte[] RemoveTrailingZeros(byte[] input) {
            var lastIndex = Array.FindLastIndex(input, b => b != 0x00);
            var result = new byte[lastIndex + 1];
            Array.Copy(input, result, lastIndex + 1);
            return result;
        }
    }
}