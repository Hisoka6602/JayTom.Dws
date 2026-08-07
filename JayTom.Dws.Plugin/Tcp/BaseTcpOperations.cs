using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;

namespace JayTom.Dws.Plugin.Tcp {

    public class BaseTcpOperations : ITcpOperations {
        private ITcpCommClient _tcpCommClient;
        private ITcpCommServer _tcpCommServer;

        public FormatType FormatType { get; set; }
        public ConnectionStatus ConnectionStatus => ConnectionType == ConnectionType.Client ? _tcpCommClient.ConnectionStatus : _tcpCommServer.ConnectionStatus;

        public event EventHandler<string>? ConnectionException;

        public event EventHandler<Exception>? Exception;

        public event EventHandler<string>? Disconnected;

        public event EventHandler<CommunicationInfo>? Communication;

        public event EventHandler<string>? Connected;

        public event EventHandler<Exception>? SendError;

        public BaseTcpOperations(ITcpCommClient tcpCommClient, ITcpCommServer tcpCommServer) {
            _tcpCommClient = tcpCommClient;
            _tcpCommClient.Exception += delegate (object? sender, Exception exception) {
                OnException(exception);
            };
            _tcpCommClient.Connected += delegate (object? sender, string s) {
                OnConnected(s);
            };
            _tcpCommClient.ConnectionException += delegate (object? sender, string s) {
                OnConnectionException(s);
            };
            _tcpCommClient.Communication += delegate (object? sender, CommunicationInfo info) {
                OnCommunication(info);
            };
            _tcpCommClient.Disconnected += delegate (object? sender, string s) {
                OnDisconnected(s);
            };
            _tcpCommClient.SendError += delegate (object? sender, Exception exception) {
                OnSendError(exception);
            };
            //注册事件
            _tcpCommServer = tcpCommServer;
            _tcpCommServer.Exception += delegate (object? sender, Exception exception) {
                OnException(exception);
            };
            _tcpCommServer.Connected += delegate (object? sender, string s) {
                OnConnected(s);
            };
            _tcpCommServer.ConnectionException += delegate (object? sender, string s) {
                OnConnectionException(s);
            };
            _tcpCommServer.Communication += delegate (object? sender, CommunicationInfo info) {
                OnCommunication(info);
            };
            _tcpCommServer.Disconnected += delegate (object? sender, string s) {
                OnDisconnected(s);
            };
            _tcpCommServer.SendError += delegate (object? sender, Exception exception) {
                OnSendError(exception);
            };
            //注册事件
            TcpServer = _tcpCommServer;
            TcpClient = _tcpCommClient;
        }

        public async Task<bool> Connect(string ipAddress, int port, int timeOut = 1000, FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default) {
            FormatType = dataType;
            if (ConnectionType == ConnectionType.Client) {
                //客户端
                return await _tcpCommClient.Connect(ipAddress, port, timeOut, dataType, dataLen, token);
            }
            else {
                return await _tcpCommServer.Connect(ipAddress, port, timeOut, dataType, dataLen, token);
            }
        }

        public async Task<bool> Reconnect(int count, CancellationToken token = default) {
            if (ConnectionType == ConnectionType.Client) {
                //客户端
                return await _tcpCommClient.Reconnect(count, token);
            }
            else {
                return await _tcpCommServer.Reconnect(count, token);
            }
        }

        public async Task<bool> SendMessage(string message, CancellationToken token = default) {
            if (ConnectionType == ConnectionType.Client) {
                //客户端
                return await _tcpCommClient.SendMessage(message, token);
            }
            else {
                return await _tcpCommServer.SendMessage(message, token);
            }
        }

        public async Task<bool> SendMessage(byte[] message, CancellationToken token = default) {
            if (ConnectionType == ConnectionType.Client) {
                //客户端
                return await _tcpCommClient.SendMessage(message, token);
            }
            else {
                return await _tcpCommServer.SendMessage(message, token);
            }
        }

        public void Close() {
            if (ConnectionType == ConnectionType.Client) {
                //客户端
                _tcpCommClient.Close();
            }
            else {
                _tcpCommServer.Close();
            }
        }

        public ConnectionType ConnectionType { get; set; } = ConnectionType.Client;
        public ITcpCommServer? TcpServer { get; private set; }
        public ITcpCommClient? TcpClient { get; private set; }

        public async Task<bool> Connect(string ipAddress, int port, ConnectionType type, int timeOut = 1000, FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default) {
            FormatType = dataType;
            ConnectionType = type;
            if (ConnectionType == ConnectionType.Client) {
                //客户端
                return await _tcpCommClient.Connect(ipAddress, port, timeOut, dataType, dataLen, token);
            }
            else {
                return await _tcpCommServer.Connect(ipAddress, port, timeOut, dataType, dataLen, token);
            }
        }

        protected virtual void OnConnectionException(string e) {
            ConnectionException?.Invoke(this, e);
        }

        protected virtual void OnException(Exception e) {
            Exception?.Invoke(this, e);
        }

        protected virtual void OnDisconnected(string e) {
            Disconnected?.Invoke(this, e);
        }

        protected virtual void OnCommunication(CommunicationInfo e) {
            Communication?.Invoke(this, e);
        }

        protected virtual void OnConnected(string e) {
            Connected?.Invoke(this, e);
        }

        protected virtual void OnSendError(Exception e) {
            SendError?.Invoke(this, e);
        }

        public byte[] ConvertHexStringToByteArray(string hexString) {
            try {
                hexString = hexString.Replace(" ", ""); // 移除空格

                var length = hexString.Length;
                var byteArray = new byte[length / 2];

                for (var i = 0; i < length; i += 2) {
                    byteArray[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
                }

                return byteArray;
            }
            catch (Exception e) {
                return [];
            }
        }
    }
}
