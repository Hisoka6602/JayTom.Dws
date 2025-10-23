using Microsoft.AspNetCore.SignalR.Client;

namespace JayTom.Dws.CrossCutting.SignalR {

    public class BaseClientMessageHub : IBaseClientMessageHub {
        private HubConnection? _hubConnection = null;
        private bool _isReconnecting = true;
        private string _computerName = string.Empty;
        private string _programName = string.Empty;
        private string _module = string.Empty;
        private string _remarks = string.Empty;
        public bool IsConnected { get; private set; } = false;

        public string ConnectionId { get; private set; } = string.Empty;

        public bool AutoReconnect { get; set; }

        public event Func<Exception, Task>? Closed;

        public event Func<string, Task>? Reconnected;

        public event Func<Exception, Task>? Reconnecting;

#pragma warning disable CS0067 // Event is never used - This is part of the public API
        public event Func<object, Task>? ReceiveMessage;
#pragma warning restore CS0067

        public async Task StartAsync(string url, Action<HubConnection> registerMethod, string computerName, string programName = "", string module = "", string remarks = "", CancellationToken token = default) {
            try {
                _computerName = computerName;
                _programName = programName;
                _module = module;
                _remarks = remarks;
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
                registerMethod.Invoke(_hubConnection);
                await _hubConnection?.StartAsync(token)!;
                ConnectionId = _hubConnection?.ConnectionId ?? string.Empty;
                IsConnected = true;
                await SetClientInfo(_computerName, _programName, _module, _remarks);
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

        public async Task SetClientInfo(string computerName, string programName, string module, string remarks) {
            try {
                if (IsConnected && _hubConnection is not null) {
                    await _hubConnection.SendCoreAsync("SetClientInfo", new object?[]
                    {
                        new HubClientInfo
                        {
                            ComputerName = computerName,
                            Module = module,
                            ProgramName = programName,
                            Remarks = remarks
                        }
                    });
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
        }

        public async Task<KeyValuePair<bool, string>> SendMessage<T>(string method, T message) {
            try {
                if (IsConnected && _hubConnection is not null) {
                    await _hubConnection.SendCoreAsync("MessageAll", new object?[]
                    {
                        method,
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

        public async Task<KeyValuePair<bool, string>> SendMessage<T>(string client, string method, T message) {
            try {
                if (IsConnected && _hubConnection is not null) {
                    await _hubConnection.SendCoreAsync("MessageToClient", new object?[]
                    {
                        client,
                        method,
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

        public async Task<KeyValuePair<bool, string>> SendMessage<T>(List<string> clients, string method, T message) {
            try {
                if (IsConnected && _hubConnection is not null) {
                    await _hubConnection.SendCoreAsync("MessageToClients", new object?[]
                    {
                        clients,
                        method,
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

        public async Task<KeyValuePair<bool, string>> SendMessageToGroup<T>(string clientGroup, string method, T message) {
            try {
                if (IsConnected && _hubConnection is not null) {
                    await _hubConnection.SendCoreAsync("MessageToGroup", new object?[]
                    {
                        clientGroup,
                        method,
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

        public async Task<List<HubClientInfo>> GetOnlineClients() {
            try {
                if (IsConnected && _hubConnection is not null) {
                    if (IsConnected) {
                        // 调用服务端的 GetOnlineClients 方法并返回结果
                        return await _hubConnection.InvokeCoreAsync<List<HubClientInfo>>("GetOnlineClients", args: new object[] { });
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return new List<HubClientInfo>();
        }

        public async Task<HubServerInfo> GetServerInfo() {
            try {
                if (IsConnected && _hubConnection is not null) {
                    if (IsConnected) {
                        // 调用服务端的 GetOnlineClients 方法并返回结果
                        return await _hubConnection.InvokeCoreAsync<HubServerInfo>("GetServerInfo", args: new object[] { });
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return new HubServerInfo();
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
            NLog.LogManager.GetCurrentClassLogger().Error($"重连成功:id={_hubConnection?.ConnectionId}");
            IsConnected = true;
            ConnectionId = _hubConnection?.ConnectionId ?? string.Empty;
            await SetClientInfo(_computerName, _programName, _module, _remarks);
            Reconnected?.Invoke(arg ?? string.Empty);
        }
    }
}