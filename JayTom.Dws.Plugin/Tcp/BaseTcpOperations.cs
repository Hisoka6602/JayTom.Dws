using System;
using System.Linq;
using System.Text;
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

        public async Task<bool> Connect(string ipAddress, int port, int timeOut = 1000, FormatType dataType = FormatType.Ascii, CancellationToken token = default) {
            FormatType = dataType;
            if (ConnectionType == ConnectionType.Client) {
                //客户端
                return await _tcpCommClient.Connect(ipAddress, port, timeOut, dataType, token);
            }
            else {
                return await _tcpCommServer.Connect(ipAddress, port, timeOut, dataType, token);
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

        public async Task<bool> Connect(string ipAddress, int port, ConnectionType type, int timeOut = 1000, FormatType dataType = FormatType.Ascii, CancellationToken token = default) {
            FormatType = dataType;
            ConnectionType = type;
            if (ConnectionType == ConnectionType.Client) {
                //客户端
                return await _tcpCommClient.Connect(ipAddress, port, timeOut, dataType, token);
            }
            else {
                return await _tcpCommServer.Connect(ipAddress, port, timeOut, dataType, token);
            }
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

            Disconnected?.Invoke(this, e);
        }

        protected virtual async void OnCommunication(CommunicationInfo e) {
            await Task.Yield();
            Communication?.Invoke(this, e);
        }

        protected virtual async void OnConnected(string e) {
            await Task.Yield();

            Connected?.Invoke(this, e);
        }

        protected virtual async void OnSendError(Exception e) {
            await Task.Yield();
            SendError?.Invoke(this, e);
        }
    }
}