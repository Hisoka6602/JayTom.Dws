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

    /// <summary>
    /// 基于TouchSocket的TCP客户端实现
    /// 支持自动重连和手动重连功能
    /// 自动重连：首次连接成功后，中途断开才进入后台重连，避免配置错误时启动阶段无限刷异常
    /// 手动重连：通过Reconnect方法，传入count<=0实现无限手动重连
    /// </summary>
    public class TouchSocketTcpClient : ITcpCommClient {
        private TouchSocket.Sockets.TcpClient? _tcpClient;
        /// <summary>
        /// 自动重连同步锁。
        /// </summary>
        private readonly object _reconnectSync = new();

        /// <summary>
        /// 发送同步锁。
        /// </summary>
        private readonly SemaphoreSlim _sendSlim = new(1, 1);

        /// <summary>
        /// 连接同步锁，避免并发连接同一个客户端实例。
        /// </summary>
        private readonly SemaphoreSlim _connectSlim = new(1, 1);

        /// <summary>
        /// 自动重连取消源。
        /// </summary>
        private CancellationTokenSource? _reconnectCancellation;

        /// <summary>
        /// 自动重连后台任务。
        /// </summary>
        private Task? _reconnectTask;

        /// <summary>
        /// 当前是否正在主动关闭连接。
        /// </summary>
        private bool _isClosing;

        /// <summary>
        /// 是否至少成功连接过一次。
        /// </summary>
        private bool _hasConnectedOnce;

        public string IpAddress { get; private set; } = string.Empty;
        public int Port { get; private set; } = 0;
        private int _dataLen = 0;

        public int DataLen {
            get => _dataLen;
            set => _dataLen = value;
        }

        public FormatType FormatType { get; set; }
        public ConnectionStatus ConnectionStatus { get; private set; } = ConnectionStatus.Disconnected;

        public event EventHandler<string>? ConnectionException;

        public event EventHandler<Exception>? Exception;

        public event EventHandler<string>? Disconnected;

        public event EventHandler<CommunicationInfo>? Communication;

        public event EventHandler<string>? Connected;

        public event EventHandler<Exception>? SendError;

        public async Task<bool> Connect(string ipAddress, int port, int timeOut = 1000, FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default) {
            _isClosing = false;
            FormatType = dataType;
            var parameter = SetParameter(new TcpConnectParam() {
                Address = ipAddress,
                Port = port,
                DataFormatType = dataType,
                DataLength = dataLen
            });
            DataLen = dataLen;
            if (parameter) {
                return await ConnectCore(dataType, dataLen, timeOut, token);
            }

            return false;
        }

        public async Task<bool> Connect(FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default) {
            _isClosing = false;
            return await ConnectCore(dataType, dataLen, 1000, token);
        }

        private async Task<bool> ConnectCore(FormatType dataType = FormatType.Ascii, int dataLen = 0, int timeOut = 1000, CancellationToken token = default) {
            await _connectSlim.WaitAsync(token);
            try {
                DataLen = dataLen;
                FormatType = dataType;
                if (ConnectionStatus == ConnectionStatus.Connected) {
                    return true;
                }

                if (_tcpClient is null) {
                    _tcpClient = new TouchSocket.Sockets.TcpClient();
                    _tcpClient.Received += delegate (TouchSocket.Sockets.TcpClient client, ByteBlock block, IRequestInfo info) {
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
                    _tcpClient.Connected += delegate {
                        OnConnected($"IpAddress:{IpAddress},Port:{Port}");
                    };
                    _tcpClient.Disconnected += delegate (ITcpClientBase client, DisconnectEventArgs args) {
                        OnDisconnected($"IpAddress:{IpAddress},Port:{Port}");
                        NLog.LogManager.GetCurrentClassLogger().Error($"断开:IpAddress:{IpAddress},Port:{Port},ConnectionStatus:{ConnectionStatus}");
                        StartAutoReconnect();
                    };
                }

                var connectTimeout = Math.Max(timeOut, 1000);
                var tcpClient = await _tcpClient.ConnectAsync(connectTimeout);
                if (tcpClient is not null &&
                    tcpClient.IP.Equals(IpAddress) &&
                    tcpClient.Port.Equals(Port)) {
                    _hasConnectedOnce = true;
                    ConnectionStatus = ConnectionStatus.Connected;
                    return true;
                }
            }
            catch (Exception e) {
                ConnectionStatus = ConnectionStatus.Disconnected;
                OnConnectionException($"TCP客户端连接失败:IpAddress:{IpAddress},Port:{Port},Message:{e.Message}");
                OnException(e);
                return false;
            }
            finally {
                _connectSlim.Release();
            }
            return false;
        }

        public async Task<bool> Reconnect(int count, CancellationToken token = default) {
            if (count > 0) {
                for (var i = 0; i < count; i++) {
                    try {
                        await Task.Delay(500, token);
                    }
                    catch (TaskCanceledException) {
                        return false;
                    }
                    await Connect(token: token);
                    if (ConnectionStatus == ConnectionStatus.Connected) {
                        return true;
                    }
                }
            }
            else {
                // Unlimited reconnection when count <= 0
                while (!token.IsCancellationRequested) {
                    try {
                        await Task.Delay(500, token);
                    }
                    catch (TaskCanceledException) {
                        return false;
                    }
                    await Connect(token: token);
                    if (ConnectionStatus == ConnectionStatus.Connected) {
                        return true;
                    }
                }
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
                        _tcpClient.Connected += delegate {
                            OnConnected($"IpAddress:{IpAddress},Port:{Port}");
                        };
                        _tcpClient.Disconnected += delegate (ITcpClientBase client, DisconnectEventArgs args) {
                            OnDisconnected($"IpAddress:{IpAddress},Port:{Port}");
                            NLog.LogManager.GetCurrentClassLogger().Error($"断开:IpAddress:{IpAddress},Port:{Port},ConnectionStatus:{ConnectionStatus}");
                            StartAutoReconnect();
                        };
                    }
                    var touchSocketConfig = new TouchSocketConfig()
                        .SetRemoteIPHost(new IPHost($"{tcpConnect.Address}:{tcpConnect.Port}"))
                        .SetBufferLength(tcpConnect.DataLength);

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
            var lockTaken = false;
            try {
                if (ConnectionStatus == ConnectionStatus.Connected) {
                    await _sendSlim.WaitAsync(token);
                    lockTaken = true;
                    if (_tcpClient is null) {
                        return false;
                    }
                    await _tcpClient.SendAsync(message);
                    OnCommunication(new CommunicationInfo() {
                        Content = message,
                        Time = DateTime.Now,
                        Type = CommunicationType.Send
                    });
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
                if (lockTaken) {
                    _sendSlim.Release();
                }
            }
            return false;
        }

        public async Task<bool> SendMessage(byte[] message, CancellationToken token = default) {
            var lockTaken = false;
            try {
                if (ConnectionStatus == ConnectionStatus.Connected) {
                    await _sendSlim.WaitAsync(token);
                    lockTaken = true;
                    if (_tcpClient is null) {
                        return false;
                    }
                    await _tcpClient.SendAsync(message);
                    OnCommunication(new CommunicationInfo() {
                        Content = BitConverter.ToString(message).Replace("-", ", "),
                        Time = DateTime.Now,
                        Type = CommunicationType.Send
                    });
                    return true;
                }
                else {
                    OnException(
                        new Exception($"IpAddress:{IpAddress},Port:{Port},ConnectionStatus:{ConnectionStatus}"));
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
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
            _isClosing = true;
            StopAutoReconnect();
            _tcpClient?.Close();
            _tcpClient = null;
            _hasConnectedOnce = false;
            ConnectionStatus = ConnectionStatus.Disconnected;
        }

        /// <summary>
        /// 启动自动重连，仅在曾经成功连接且不是主动关闭时生效。
        /// </summary>
        private void StartAutoReconnect() {
            lock (_reconnectSync) {
                if (_isClosing || !_hasConnectedOnce) {
                    return;
                }

                if (_reconnectTask is { IsCompleted: false }) {
                    return;
                }

                _reconnectCancellation?.Dispose();
                _reconnectCancellation = new CancellationTokenSource();
                var token = _reconnectCancellation.Token;
                _reconnectTask = Task.Run(() => RunAutoReconnectAsync(token), CancellationToken.None);
            }
        }

        /// <summary>
        /// 停止自动重连。
        /// </summary>
        private void StopAutoReconnect() {
            CancellationTokenSource? cancellation;
            Task? reconnectTask;
            lock (_reconnectSync) {
                cancellation = _reconnectCancellation;
                reconnectTask = _reconnectTask;
                _reconnectCancellation = null;
                _reconnectTask = null;
            }

            if (cancellation is null) {
                return;
            }

            cancellation.Cancel();
            if (reconnectTask is null) {
                cancellation.Dispose();
            }
            else {
                _ = reconnectTask.ContinueWith(_ => cancellation.Dispose(),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }

        /// <summary>
        /// 执行自动重连循环。
        /// </summary>
        private async Task RunAutoReconnectAsync(CancellationToken token) {
            try {
                while (!_isClosing &&
                       !token.IsCancellationRequested &&
                       ConnectionStatus != ConnectionStatus.Connected) {
                    await Task.Delay(1000, token);

                    if (_isClosing || token.IsCancellationRequested) {
                        return;
                    }

                    await ConnectCore(FormatType, DataLen, 5000, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) {
                // 主动停止自动重连。
            }
            catch (Exception e) {
                OnException(e);
            }
        }

        protected virtual void OnConnectionException(string e) {
            ConnectionException?.Invoke(this, e);
        }

        protected virtual void OnException(Exception e) {
            Exception?.Invoke(this, e);
        }

        protected virtual void OnDisconnected(string e) {
            ConnectionStatus = ConnectionStatus.Disconnected;
            Disconnected?.Invoke(this, e);
        }

        protected virtual void OnCommunication(CommunicationInfo e) {
            Communication?.Invoke(this, e);
        }

        protected virtual void OnConnected(string e) {
            _hasConnectedOnce = true;
            ConnectionStatus = ConnectionStatus.Connected;
            Connected?.Invoke(this, e);
        }

        protected virtual void OnSendError(Exception e) {
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
