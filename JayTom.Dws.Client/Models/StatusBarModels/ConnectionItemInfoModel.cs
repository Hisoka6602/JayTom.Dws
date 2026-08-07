using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.StatusBarModels
{

    /// <summary>
    /// 连接信息类
    /// </summary>
    public class ConnectionItemInfoModel : BindableBase
    {
        private string _connectionName = string.Empty;
        private ConnectionType _connectionType = ConnectionType.None;
        private ConnectionState _connectionState = ConnectionState.Disconnected;

        public string ConnectionName
        {
            get => _connectionName;
            set => SetProperty(ref _connectionName, value);
        }

        public ConnectionType ConnectionType
        {
            get => _connectionType;
            set => SetProperty(ref _connectionType, value);
        }

        public ConnectionState ConnectionState
        {
            get => _connectionState;
            set => SetProperty(ref _connectionState, value);
        }
    }

    public class ConnectionItemInfoModelComparer : IEqualityComparer<ConnectionItemInfoModel>
    {

        public bool Equals(ConnectionItemInfoModel? x, ConnectionItemInfoModel? y)
        {
            return x != null && x?.ConnectionName == y?.ConnectionName && x?.ConnectionType == y?.ConnectionType;
        }

        public int GetHashCode(ConnectionItemInfoModel obj)
        {
            return obj.ConnectionName.GetHashCode() ^ obj.ConnectionType.GetHashCode();
        }
    }

    public enum ConnectionType
    {
        None,

        /// <summary>
        /// Tcp
        /// </summary>
        TCP,

        /// <summary>
        /// 串口
        /// </summary>
        SerialPort,

        /// <summary>
        /// 音频
        /// </summary>
        Audio,

        /// <summary>
        /// 位置
        /// </summary>
        Location,

        /// <summary>
        /// Ftp
        /// </summary>
        FTP,

        /// <summary>
        /// 控件
        /// </summary>
        Custom
    }

    public enum ConnectionState
    {

        /// <summary>
        /// 未连接
        /// </summary>
        Disconnected,

        /// <summary>
        /// 正在连接
        /// </summary>
        Connecting,

        /// <summary>
        /// 连接失败
        /// </summary>
        ConnectionFailed,

        /// <summary>
        /// 连接成功
        /// </summary>
        Connected
    }
}