using Microsoft.AspNetCore.SignalR;

namespace JayTom.Dws.CrossCutting.SignalR {

    public interface IBaseServerMessageHub {

        /// <summary>
        /// 连接加入
        /// </summary>
        public event Func<HubClientInfo, Task> UserConnected;

        /// <summary>
        /// 连接退出
        /// </summary>
        public event Func<HubClientInfo, Task> UserDisconnected;

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="excludedClients">要排除的客户端列表</param>
        [HubMethodName("Stop")]
        void Stop(List<string> excludedClients);

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="excludedClients">要排除的客户端列表</param>
        [HubMethodName("Start")]
        void Start(List<string> excludedClients);

        /// <summary>
        /// 退出
        /// </summary>
        /// <param name="excludedClients">要排除的客户端列表</param>
        [HubMethodName("Exit")]
        void Exit(List<string> excludedClients);

        /// <summary>
        /// 客户端提交信息
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="module"></param>
        /// <param name="remarks"></param>
        /// <param name="programName"></param>
        /// <returns></returns>
        [HubMethodName("SetClientInfo")]
        Task SetClientInfo(string computerName, string programName, string module, string remarks);

        /// <summary>
        /// 发送消息给所有客户端
        /// </summary>
        /// <param name="method"></param>
        /// <param name="message">消息内容</param>
        [HubMethodName("MessageAll")]
        void MessageAll(string method, object message);

        /// <summary>
        /// 发送消息给指定客户端
        /// </summary>
        /// <param name="client">客户端标识</param>
        /// <param name="method"></param>
        /// <param name="message">消息内容</param>
        [HubMethodName("MessageToClient")]
        void MessageToClient(string client, string method, object message);

        /// <summary>
        /// 发送消息给指定客户端列表
        /// </summary>
        /// <param name="clients">客户端标识列表</param>
        /// <param name="method"></param>
        /// <param name="message">消息内容</param>
        [HubMethodName("MessageToClients")]
        void MessageToClients(List<string> clients, string method, object message);

        /// <summary>
        /// 发送消息给指定客户端组
        /// </summary>
        /// <param name="clientGroup">客户端组标识</param>
        /// <param name="method"></param>
        /// <param name="message">消息内容</param>
        [HubMethodName("MessageToGroup")]
        void SendMessageToGroup(string clientGroup, string method, object message);

        /// <summary>
        /// 获取在线客户端列表
        /// </summary>
        /// <returns>在线客户端标识列表</returns>
        [HubMethodName("GetOnlineClients")]
        List<HubClientInfo> GetOnlineClients();

        /// <summary>
        /// 获取服务信息
        /// </summary>
        /// <returns>服务信息</returns>
        [HubMethodName("GetServerInfo")]
        HubServerInfo GetServerInfo();

        /// <summary>
        /// 设置服务信息
        /// </summary>
        void SetServerInfo(string serverName, string remarks);
    }

    /// <summary>
    /// 客户端信息
    /// </summary>
    public class HubClientInfo {

        /// <summary>
        /// 连接ID
        /// </summary>
        public string ConnectionId { get; set; } = string.Empty;

        /// <summary>
        /// 计算机名称
        /// </summary>
        public string ComputerName { get; set; } = string.Empty;

        /// <summary>
        /// 程序名称
        /// </summary>
        public string ProgramName { get; set; } = string.Empty;

        /// <summary>
        /// 模块
        /// </summary>
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// 连接时间
        /// </summary>
        public DateTime ConnectionTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        public HubClientInfo() {
        }

        public HubClientInfo(string connectionId, string computerName, DateTime connectionTime, string remarks) {
            ConnectionId = connectionId;
            ComputerName = computerName;
            ConnectionTime = connectionTime;
            Remarks = remarks;
        }

        public override string ToString() {
            return $"ConnectionId: {ConnectionId}, ComputerName: {ComputerName}, ConnectionTime: {ConnectionTime}, Remarks: {Remarks}";
        }
    }

    /// <summary>
    /// 服务端信息
    /// </summary>
    public class HubServerInfo {

        /// <summary>
        /// 服务端名称
        /// </summary>
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// 启动时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 运行时长
        /// </summary>
        public TimeSpan Uptime => DateTime.Now - StartTime;

        /// <summary>
        /// 当前客户端集合
        /// </summary>
        public List<HubClientInfo> CurrentClients { get; set; } = new();

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        public HubServerInfo() {
        }

        public HubServerInfo(string serverName, DateTime startTime, List<HubClientInfo>? currentClients, string remarks) {
            ServerName = serverName;
            StartTime = startTime;
            CurrentClients = currentClients ?? new List<HubClientInfo>();
            Remarks = remarks;
        }

        public override string ToString() {
            return $"ServerName: {ServerName}, StartTime: {StartTime}, Uptime: {Uptime}, CurrentClientsCount: {CurrentClients.Count}, Remarks: {Remarks}";
        }
    }
}