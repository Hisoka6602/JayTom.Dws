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
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.ComponentModel.DataAnnotations;
using JayTom.Dws.Plugin;

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
        /// <summary>
        /// 按客户端隔离的固定长度消息分帧缓冲区。
        /// </summary>
        private readonly ConcurrentDictionary<string, ReceiveBufferState> _receiveBuffers = new();
        /// <summary>
        /// 收包回调只拆帧和入队，协议解析及业务订阅在专用消费者中执行。
        /// 设备指令不允许因队列繁忙被丢弃或反向阻塞 Socket 回调。
        /// </summary>
        private readonly Channel<(string ClientKey, byte[] Frame, DateTime ReceivedTime)> _receivedFrames =
            Channel.CreateUnbounded<(string ClientKey, byte[] Frame, DateTime ReceivedTime)>(new UnboundedChannelOptions {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        /// <summary>严格按入队顺序发布完整帧的专用消费者。</summary>
        private readonly Task _receivedFrameWorker;

        /// <summary>创建 TCP 服务端并启动专用收帧消费者。</summary>
        public TouchSocketTcpServer() {
            _receivedFrameWorker = ProcessReceivedFramesAsync();
        }

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
                            HandleReceivedBytes(client.ID, block.Buffer, block.Len, DateTime.Now);
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
                        _receiveBuffers.TryRemove(client.ID, out _);
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
                                HandleReceivedBytes(client.ID, block.Buffer, block.Len, DateTime.Now);
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
                            _receiveBuffers.TryRemove(client.ID, out _);
                            OnDisconnected($"{client.ID}-{client.IP}:{client.Port} Disconnected");
                        };
                    }

                    var listenIpHosts = new TouchSocketConfig().SetListenIPHosts([new($"{IPAddress.Any.ToString()}:{tcpConnect.Port}")])
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
                        return true;
                    }
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
                            Content = HexDataFormatter.Format(message),
                            Time = DateTime.Now,
                            Type = CommunicationType.Send
                        });
                        return true;
                    }
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
                _receiveBuffers.Clear();
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
            set {
                var normalizedValue = Math.Max(0, value);
                if (_dataLen != normalizedValue) {
                    _receiveBuffers.Clear();
                }
                _dataLen = normalizedValue;
            }
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
                                Content = HexDataFormatter.Format(message),
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
            e.FormatType = FormatType;
            if (FormatType == FormatType.Hex) {
                e.Content = HexDataFormatter.Normalize(e.Content);
            }
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

        /// <summary>
        /// 处理指定客户端的 TCP 粘包和拆包。
        /// </summary>
        private void HandleReceivedBytes(
            string clientKey,
            byte[] buffer,
            int length,
            DateTime receivedTime) {
            if (length <= 0) {
                return;
            }

            var frames = new List<byte[]>();
            if (DataLen <= 0) {
                frames.Add(buffer.AsSpan(0, length).ToArray());
            }
            else {
                var state = _receiveBuffers.GetOrAdd(clientKey, static _ => new ReceiveBufferState());
                lock (state.SyncRoot) {
                    for (var index = 0; index < length; index++) {
                        state.Buffer.Add(buffer[index]);
                    }

                    while (state.Buffer.Count - state.Offset >= DataLen) {
                        var frame = new byte[DataLen];
                        state.Buffer.CopyTo(state.Offset, frame, 0, DataLen);
                        state.Offset += DataLen;
                        frames.Add(frame);
                    }

                    if (state.Offset == state.Buffer.Count) {
                        state.Buffer.Clear();
                        state.Offset = 0;
                    }
                    else if (state.Offset >= 4096) {
                        state.Buffer.RemoveRange(0, state.Offset);
                        state.Offset = 0;
                    }
                }
            }

            foreach (var frame in frames) {
                if (!_receivedFrames.Writer.TryWrite(
                    (clientKey, frame, receivedTime))) {
                    OnException(new InvalidOperationException(
                        $"TCP服务端接收帧队列已停止，客户端 {clientKey} 的完整报文未能入队"));
                }
            }
        }

        /// <summary>按入队顺序发布完整帧，避免 Socket 回调执行协议和业务逻辑。</summary>
        private async Task ProcessReceivedFramesAsync() {
            await foreach (var receivedFrame in _receivedFrames.Reader.ReadAllAsync()
                               .ConfigureAwait(false)) {
                try {
                    OnCommunication(new CommunicationInfo {
                        Content = FormatType == FormatType.Ascii
                            ? Encoding.Default.GetString(receivedFrame.Frame)
                            : HexDataFormatter.Format(receivedFrame.Frame),
                        Time = receivedFrame.ReceivedTime,
                        Type = CommunicationType.Receive
                    });
                }
                catch (Exception exception) {
                    OnException(exception);
                }
            }
        }

        private sealed class ReceiveBufferState {
            /// <summary>
            /// 保护单客户端接收状态。
            /// </summary>
            internal object SyncRoot { get; } = new();
            /// <summary>
            /// 保存尚未组成完整固定长度帧的字节。
            /// </summary>
            internal List<byte> Buffer { get; } = new();
            /// <summary>
            /// 下一帧在缓冲区中的读取偏移。
            /// </summary>
            internal int Offset { get; set; }
        }

        public byte[] RemoveTrailingZeros(byte[] input) {
            int lastIndex = Array.FindLastIndex(input, b => b != 0x00);
            byte[] result = new byte[lastIndex + 1];
            Array.Copy(input, result, lastIndex + 1);
            return result;
        }
    }
}
