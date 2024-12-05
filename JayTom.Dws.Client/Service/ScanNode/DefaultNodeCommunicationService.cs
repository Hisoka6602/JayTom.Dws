using System;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Tcp.TcpClient;

namespace JayTom.Dws.Client.Service.ScanNode {
    public class DefaultNodeCommunicationService : INodeCommunicationService {
        private static List<NodeCommunicationInfo> _listeningNodes = new();
        private static List<TouchSocketTcpClient> _tcpClient = new();
        private SemaphoreSlim _semaphore = new(1);

        public event EventHandler<NodeCommunicationInfo>? NodeConnected;

        public event EventHandler<NodeCommunicationInfo>? NodeDisconnected;

        public event EventHandler<NodeReceivedEventArgs>? DataReceived;

        public List<NodeCommunicationInfo> GetAllListeningNodes() => _listeningNodes;

        public async Task ConnectedListeningNodes(List<NodeCommunicationInfo> nodes) {
            foreach (var nodeCommunicationInfo in nodes) {
                await AddListeningNode(nodeCommunicationInfo);
            }
        }

        public async Task<KeyValuePair<bool, string>> AddListeningNode(NodeCommunicationInfo node) {
            var touchSocketTcpClient = _tcpClient.FirstOrDefault(f => f.IpAddress.Equals(node.IpAddress) &&
                                                                      f.Port.Equals(node.Port));
            if (touchSocketTcpClient is not null) {
                if (touchSocketTcpClient.ConnectionStatus == ConnectionStatus.Connected) {
                    return new KeyValuePair<bool, string>(true, "监听已存在");
                }

                var connect = await touchSocketTcpClient.Connect();
                return new KeyValuePair<bool, string>(connect, $"连接{(connect ? "成功" : "失败")}");
            }

            var socketTcpClient = new TouchSocketTcpClient();
            socketTcpClient.Disconnected += (sender, s) => {
                OnNodeDisconnected(new NodeCommunicationInfo() {
                    IpAddress = socketTcpClient.IpAddress,
                    Port = socketTcpClient.Port,
                    IsOnline = false
                });
            };
            socketTcpClient.Connected += (sender, s) => {
                OnNodeConnected(new NodeCommunicationInfo() {
                    IpAddress = socketTcpClient.IpAddress,
                    Port = socketTcpClient.Port,
                    IsOnline = true
                });
            };
            socketTcpClient.Communication += (sender, info) => {
                if (info.Type == CommunicationType.Receive) {
                    OnDataReceived(new NodeReceivedEventArgs() {
                        Info = new NodeCommunicationInfo() {
                            IpAddress = socketTcpClient.IpAddress,
                            Port = socketTcpClient.Port,
                            IsOnline = true
                        },
                        Massage = info.Content
                    });
                }
            };
            var b = await socketTcpClient.Connect(node.IpAddress, node.Port);
            if (b) {
                try {
                    await _semaphore.WaitAsync();
                    _tcpClient.Add(socketTcpClient);
                    _listeningNodes.Add(new NodeCommunicationInfo() {
                        IpAddress = socketTcpClient.IpAddress,
                        Port = socketTcpClient.Port,
                        IsOnline = true
                    });
                }
                finally {
                    _semaphore.Release();
                }
            }
            return new KeyValuePair<bool, string>(b, $"连接{(b ? "成功" : "失败")}");
        }

        public async Task<KeyValuePair<bool, string>> CloseListeningNode(NodeCommunicationInfo node) {
            var touchSocketTcpClient = _tcpClient.FirstOrDefault(f =>
                f.IpAddress.Equals(node.IpAddress) &&
                f.Port.Equals(node.Port));

            if (touchSocketTcpClient is not null) {
                touchSocketTcpClient.Close();
                try {
                    await _semaphore.WaitAsync();
                    _listeningNodes.Remove(node);
                    _tcpClient.Remove(touchSocketTcpClient);
                }
                finally {
                    _semaphore.Release();
                }

                return new KeyValuePair<bool, string>(true, "关闭成功");
            }
            return new KeyValuePair<bool, string>(false, "关闭失败");
        }

        public async Task CloseAllListeningNodes() {
            for (var i = _listeningNodes.Count - 1; i >= 0; i--) {
                await CloseListeningNode(_listeningNodes[i]);

            }

        }

        protected virtual async void OnNodeDisconnected(NodeCommunicationInfo e) {
            try {
                await _semaphore.WaitAsync();
                var nodeCommunicationInfo = _listeningNodes.FirstOrDefault(f => f.IpAddress.Equals(e.IpAddress) &&
                    f.Port.Equals(e.Port));
                if (nodeCommunicationInfo is not null) {
                    nodeCommunicationInfo.IsOnline = false;
                }
            }
            finally {
                _semaphore.Release();
            }
            NodeDisconnected?.Invoke(this, e);
        }

        protected virtual async void OnNodeConnected(NodeCommunicationInfo e) {
            try {
                await _semaphore.WaitAsync();
                var nodeCommunicationInfo = _listeningNodes.FirstOrDefault(f => f.IpAddress.Equals(e.IpAddress) &&
                    f.Port.Equals(e.Port));
                if (nodeCommunicationInfo is not null) {
                    nodeCommunicationInfo.IsOnline = true;
                }
            }
            finally {
                _semaphore.Release();
            }
            NodeConnected?.Invoke(this, e);
        }

        protected virtual async void OnDataReceived(NodeReceivedEventArgs e) {
            await Task.Yield();
            DataReceived?.Invoke(this, e);
        }
    }
}