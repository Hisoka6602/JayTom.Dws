using Polly.Retry;
using Microsoft.AspNetCore.SignalR.Client;

namespace JayTom.Dws.Infrastructure.SignalR.VideoApi.ClientMessageHub {

    public class ClientMessageHub : IClientMessageHub {
        private static HubConnection? _hubConnection = null;
        private static bool _isConnected = false;
        private static bool _autoReconnect;
        private static bool _isReconnecting = false;
        private static string _connectionId = string.Empty;

        public bool IsConnected {
            get => _isConnected;
            private set => _isConnected = value;
        }

        public string ConnectionId {
            get => _connectionId;
            private set => _connectionId = value;
        }

        public bool AutoReconnect {
            get => _autoReconnect;
            set => _autoReconnect = value;
        }

        public event Func<Exception, Task>? Closed;

        public event Func<string, Task>? Reconnected;

        public event Func<Exception, Task>? Reconnecting;

        public event Func<ReceiveMessageInfo, Task>? ReceiveMessage;

        public async Task StartAsync(string url, CancellationToken token = default) {
            try {
                if (IsConnected || _hubConnection is not null) return;
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(url)
                    .WithAutomaticReconnect(new RetryPolicy())
                    .Build();
                _hubConnection.ServerTimeout = TimeSpan.FromMinutes(5);
                _hubConnection.KeepAliveInterval = TimeSpan.FromSeconds(40);
                _hubConnection.HandshakeTimeout = TimeSpan.FromMinutes(2);
                _hubConnection.Closed += OnClosed;
                _hubConnection.Reconnected += OnReconnected;
                _hubConnection.Reconnecting += OnReconnecting;
                //注册接收服务函数
                RegisterMethod(_hubConnection, "DataSummaries",
                    ReceiveMessageType.DataSummaries);

                await _hubConnection?.StartAsync(token)!;
                ConnectionId = _hubConnection?.ConnectionId ?? string.Empty;
                IsConnected = true;
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                if (AutoReconnect) {
                    await OnClosed(e);
                }
            }
        }

        private async Task OnReconnecting(Exception? arg) {
            await Task.Yield();
            Reconnecting?.Invoke(arg ?? new Exception());
        }

        private async Task OnReconnected(string? arg) {
            await Task.Yield();
            NLog.LogManager.GetCurrentClassLogger().Error($"重连成功:id={_hubConnection.ConnectionId}");
            IsConnected = true;
            ConnectionId = _hubConnection?.ConnectionId ?? string.Empty;
            Reconnected?.Invoke(arg ?? string.Empty);
        }

        public async Task StopAsync(CancellationToken token = default) {
            if (_hubConnection is not null) {
                try {
                    await _hubConnection.StopAsync(token);
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                    IsConnected = false;
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
        }

        public async Task<bool> Reconnect(TimeSpan delayTime, CancellationToken token = default) {
            if (IsConnected || _hubConnection is null) return false;
            try {
                await Task.Delay(delayTime, token);
                await _hubConnection?.StartAsync(token)!;

                ConnectionId = _hubConnection?.ConnectionId ?? string.Empty;
                IsConnected = true;
                await OnReconnected($"重连成功,ConnectionId:{ConnectionId},时间:{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                return true;
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return false;
        }

        protected virtual async Task OnReceiveMessage(ReceiveMessageInfo arg) {
            await Task.Yield();
            ReceiveMessage?.Invoke(arg);
        }

        private void RegisterMethod(HubConnection hubConnection, string methodName, ReceiveMessageType type) {
            hubConnection.On<object>(methodName, async data => {
                await OnReceiveMessage(new ReceiveMessageInfo {
                    MessageData = data,
                    MessageTime = DateTime.Now,
                    MessageType = type
                });
            });
        }

        protected virtual async Task OnClosed(Exception? arg) {
            await Task.Yield();
            IsConnected = false;
            Closed?.Invoke(arg ?? new Exception());
            if (AutoReconnect && !_isReconnecting) {
                _isReconnecting = true;
                while (!IsConnected) {
                    var reconnect = await Reconnect(TimeSpan.FromSeconds(10), new CancellationToken(false));
                    if (reconnect) {
                        break;
                    }
                }
                _isReconnecting = false;
            }
        }
    }
}