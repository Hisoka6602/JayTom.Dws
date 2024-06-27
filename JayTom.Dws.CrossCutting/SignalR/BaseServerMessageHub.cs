using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace JayTom.Dws.CrossCutting.SignalR {

    public class BaseServerMessageHub : Hub, IBaseServerMessageHub {
        private readonly IHubContext<BaseServerMessageHub> _hubContext;
        private readonly ILogger<BaseServerMessageHub> _logger;
        private static readonly ConcurrentDictionary<string, HubClientInfo> Connections = new();

        private static HubServerInfo _serverInfo = new() {
            StartTime = DateTime.Now
        };

        public BaseServerMessageHub(IHubContext<BaseServerMessageHub> hubContext, ILogger<BaseServerMessageHub> logger) {
            _hubContext = hubContext;
            _logger = logger;
        }

        public override async Task OnConnectedAsync() {
            var clientInfo = new HubClientInfo {
                ConnectionId = Context.ConnectionId,
                ConnectionTime = DateTime.Now,
                Remarks = string.Empty
            };
            Connections.AddOrUpdate(Context.ConnectionId, key => clientInfo, (key, old) =>
                clientInfo);

            await OnUserConnected(clientInfo);
            _logger.LogInformation($"连接加入:{clientInfo}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception) {
            var connectionId = Context.ConnectionId;
            Connections.Remove(connectionId, out var user);
            await OnUserDisconnected(user);
            _logger.LogInformation($"连接退出:{user}");
            await base.OnDisconnectedAsync(exception);
        }

        public event Func<HubClientInfo, Task>? UserConnected;

        public event Func<HubClientInfo, Task>? UserDisconnected;

        public async void Stop(List<string> excludedClients) {
            await _hubContext.Clients.AllExcept(excludedClients).SendCoreAsync("Stop", new object?[]
            {
                "Stop"
            });
        }

        public async void Start(List<string> excludedClients) {
            await _hubContext.Clients.AllExcept(excludedClients).SendCoreAsync("Start", new object?[]
            {
                "Start"
            });
        }

        public async void Exit(List<string> excludedClients) {
            await _hubContext.Clients.AllExcept(excludedClients).SendCoreAsync("Exit", new object?[]
            {
                "Exit"
            });
        }

        public async Task SetClientInfo(string computerName, string programName, string module, string remarks) {
            var connectionId = Context.ConnectionId;
            if (Connections.TryGetValue(connectionId, out var clientInfo)) {
                clientInfo.ComputerName = computerName;
                clientInfo.ProgramName = programName;
                clientInfo.Module = module;
                clientInfo.Remarks = remarks;
                Connections[connectionId] = clientInfo;
            }
            await Task.CompletedTask;
        }

        public async void MessageAll(string method, object message) {
            await _hubContext.Clients.All.SendCoreAsync(method, new object?[]
            {
                message
            });
        }

        public async void MessageToClient(string client, string method, object message) {
            await _hubContext.Clients.Clients(client).SendCoreAsync(method, new object?[]
            {
                message
            });
        }

        public async void MessageToClients(List<string> clients, string method, object message) {
            await _hubContext.Clients.Clients(clients).SendCoreAsync(method, new object?[]
            {
                message
            });
        }

        public async void SendMessageToGroup(string clientGroup, string method, object message) {
            await _hubContext.Clients.Group(clientGroup).SendCoreAsync(method, new object?[]
            {
                message
            });
        }

        public List<HubClientInfo> GetOnlineClients() {
            return Connections.Select(s => s.Value)?.ToList() ?? new List<HubClientInfo>();
        }

        public HubServerInfo GetServerInfo() {
            _serverInfo.CurrentClients = Connections.Select(s => s.Value)?.ToList() ?? new List<HubClientInfo>();
            return _serverInfo;
        }

        public void SetServerInfo(string serverName, string remarks) {
            _serverInfo.ServerName = serverName;
            _serverInfo.Remarks = remarks;
        }

        protected virtual async Task OnUserConnected(HubClientInfo arg) {
            await Task.Yield();
            UserConnected?.Invoke(arg);
        }

        protected virtual async Task OnUserDisconnected(HubClientInfo? arg) {
            await Task.Yield();
            if (arg is not null) {
                UserDisconnected?.Invoke(arg);
            }
        }
    }
}