using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras {

    public class ScanNodeItemInfoModel : BindableBase {
        private int _num;
        private string _ipAddress = string.Empty;
        private int _port;
        private string _nodeName = string.Empty;
        private int _nodeNum;
        private int _timeout;
        private string _imagePath = string.Empty;
        private NodeStatus _status = NodeStatus.Disconnected;

        /// <summary>
        /// 序号
        /// </summary>
        public int Num {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName {
            get => _nodeName;
            set => SetProperty(ref _nodeName, value);
        }

        /// <summary>
        /// 节点序号
        /// </summary>
        public int NodeNum {
            get => _nodeNum;
            set => SetProperty(ref _nodeNum, value);
        }

        /// <summary>
        /// 赋值超时时间
        /// </summary>
        public int Timeout {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }

        /// <summary>
        /// 存图位置
        /// </summary>
        public string ImagePath {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        /// <summary>
        /// 状态
        /// </summary>
        public NodeStatus Status {
            get => _status;
            set => SetProperty(ref _status, value);
        }
    }

    public enum NodeStatus {

        /// <summary>
        /// 未连接
        /// </summary>
        [Description("未连接")]
        Disconnected,

        /// <summary>
        /// 已连接
        /// </summary>
        [Description("已连接")]
        Connected,

        /// <summary>
        /// 连接中
        /// </summary>
        [Description("连接中")]
        Connecting
    }
}