using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Tcp.TcpClient;

namespace JayTom.Dws.Client.Service.ScanNode {

    public interface INodeCommunicationService {

        //TouchSocketTcpClient
        /// <summary>
        /// 当节点连接时触发
        /// </summary>
        event EventHandler<NodeCommunicationInfo> NodeConnected;

        /// <summary>
        /// 当节点断开时触发
        /// </summary>
        event EventHandler<NodeCommunicationInfo> NodeDisconnected;

        /// <summary>
        /// 接收到数据触发
        /// </summary>
        event EventHandler<NodeReceivedEventArgs> DataReceived;

        /// <summary>
        /// 获取全部监听节点
        /// </summary>
        /// <returns></returns>
        public List<NodeCommunicationInfo> GetAllListeningNodes();

        /// <summary>
        /// 连接全部节点
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public Task ConnectedListeningNodes(List<NodeCommunicationInfo> nodes);

        /// <summary>
        /// 添加监听节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, string>> AddListeningNode(NodeCommunicationInfo node);

        /// <summary>
        /// 关闭指定节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, string>> CloseListeningNode(NodeCommunicationInfo node);

        /// <summary>
        /// 关闭全部节点
        /// </summary>
        /// <returns></returns>
        public Task CloseAllListeningNodes();
    }

    public class NodeCommunicationInfo {

        /// <summary>
        /// 获取或设置节点的 IP 地址。
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置节点的端口号。
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 获取或设置节点的状态，表示节点是否在线。
        /// </summary>
        public bool IsOnline { get; set; }
    }

    public class NodeReceivedEventArgs : EventArgs {
        public NodeCommunicationInfo? Info { get; set; }
        public string Massage { get; set; } = string.Empty;
    }
}