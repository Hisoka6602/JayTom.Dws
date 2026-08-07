using Prism.Mvvm;
using JayTom.Dws.Plugin.Tcp;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Domain.Dto.BaseInfoModels;

namespace JayTom.Dws.Client.Models.SettingsCommomModels
{

    public class TcpSettingsInfoModel : BindableBase
    {
        private TcpConnectionMode? _connectionMode;
        private TcpInfoModel _clientConfig = new();
        private TcpInfoModel _serverConfig = new();
        private DataFormatType _dataFormat = DataFormatType.Ascii;

        /// <summary>
        /// 连接模式(客户端、服务端)
        /// </summary>
        public TcpConnectionMode? ConnectionMode
        {
            get => _connectionMode;
            set => SetProperty(ref _connectionMode, value);
        }

        /// <summary>
        /// 客户端配置
        /// </summary>
        public TcpInfoModel ClientConfig
        {
            get => _clientConfig;
            set => SetProperty(ref _clientConfig, value);
        }

        /// <summary>
        /// 服务端配置
        /// </summary>
        public TcpInfoModel ServerConfig
        {
            get => _serverConfig;
            set => SetProperty(ref _serverConfig, value);
        }

        /// <summary>
        /// 数据格式
        /// </summary>
        public DataFormatType DataFormat
        {
            get => _dataFormat;
            set => SetProperty(ref _dataFormat, value);
        }
    }
}