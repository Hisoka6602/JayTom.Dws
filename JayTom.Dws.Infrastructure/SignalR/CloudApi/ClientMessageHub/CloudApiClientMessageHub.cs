using System;
using System.Linq;
using System.Text;
using RTools_NTS.Util;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Client;
using JayTom.Dws.Infrastructure.SignalR.VideoApi.ClientMessageHub;
using JayTom.Dws.Infrastructure.SignalR.CloudApi.SignalRMessageHub;

namespace JayTom.Dws.Infrastructure.SignalR.CloudApi.ClientMessageHub {

    public class CloudApiClientMessageHub : ICloudApiClientMessageHub {
        private static HubConnection? _hubConnection = null;
        private static bool _isConnected = false;
        private static bool _autoReconnect;
        private static bool _isReconnecting = true;
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

        public event Func<ClientReceiveMessageInfo, Task>? ReceiveMessage;

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
                RegisterMethod(_hubConnection, "Stop");
                RegisterMethod(_hubConnection, "Start");
                RegisterMethod(_hubConnection, "Exit");
                RegisterMethod(_hubConnection, "SyncSettingsInfo");
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

        public async Task<KeyValuePair<bool, string>> SendMessage<T>(string messageType, T message) {
            try {
                if (IsConnected && _hubConnection is not null) {
                    await _hubConnection.SendCoreAsync("Message", new object?[]
                    {
                        messageType,
                        message
                    });
                    return new KeyValuePair<bool, string>(true, "发送成功");
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return new KeyValuePair<bool, string>(true, "发送失败");
        }

        public async Task<KeyValuePair<bool, string>> SendMessage<T>(string client, string messageType, T message) {
            try {
                if (IsConnected && _hubConnection is not null) {
                    await _hubConnection.SendCoreAsync("Message", new object?[]
                    {
                        client,
                        messageType,
                        message
                    });
                    return new KeyValuePair<bool, string>(true, "发送成功");
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return new KeyValuePair<bool, string>(true, "发送失败");
        }

        public async Task<KeyValuePair<bool, string>> SendMessage<T>(List<string> clients, string messageType, T message) {
            try {
                if (IsConnected && _hubConnection is not null) {
                    await _hubConnection.SendCoreAsync("Message", new object?[]
                    {
                        clients,
                        messageType,
                        message
                    });
                    return new KeyValuePair<bool, string>(true, "发送成功");
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return new KeyValuePair<bool, string>(true, "发送失败");
        }

        public async Task<KeyValuePair<bool, string>> SendMessageToGroup<T>(string clientGroup, string messageType, T message) {
            try {
                if (IsConnected && _hubConnection is not null) {
                    await _hubConnection.SendCoreAsync("Message", new object?[]
                    {
                        clientGroup,
                        messageType,
                        message
                    });
                    return new KeyValuePair<bool, string>(true, "发送成功");
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return new KeyValuePair<bool, string>(true, "发送失败");
        }

        public async Task<bool> SyncSettingsInfo<T>(string settingsName, T message) {
            try {
                if (IsConnected && _hubConnection is not null) {
                    await _hubConnection.SendCoreAsync("SyncSettingsInfo", new object?[]
                     {
                         ConnectionId,
                        settingsName,
                        message
                     });
                    return true;
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return false;
        }

        private void RegisterMethod(HubConnection hubConnection, string methodName) {
            hubConnection.On<object>(methodName, async data => {
                await OnReceiveMessage(new ClientReceiveMessageInfo {
                    MessageData = data,
                    MessageTime = DateTime.Now,
                    MethodName = methodName
                });
            });
        }

        protected virtual async Task OnReceiveMessage(ClientReceiveMessageInfo arg) {
            await Task.Yield();
            ReceiveMessage?.Invoke(arg);
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

        protected virtual async Task OnReconnected(string? arg) {
            await Task.Yield();
            NLog.LogManager.GetCurrentClassLogger().Error($"重连成功:id={_hubConnection.ConnectionId}");
            IsConnected = true;
            ConnectionId = _hubConnection?.ConnectionId ?? string.Empty;
            Reconnected?.Invoke(arg ?? string.Empty);
        }
    }
}