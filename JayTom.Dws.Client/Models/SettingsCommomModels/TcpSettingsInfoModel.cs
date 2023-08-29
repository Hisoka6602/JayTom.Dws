using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;

namespace JayTom.Dws.Client.Models.SettingsCommomModels {

    public class TcpSettingsInfoModel : BindableBase {
        private TcpConnectionMode? _connectionMode;
        private TcpInfoModel _clientConfig = new();
        private TcpInfoModel _serverConfig = new();

        /// <summary>
        /// 连接模式(客户端、服务端)
        /// </summary>
        public TcpConnectionMode? ConnectionMode {
            get => _connectionMode;
            set => SetProperty(ref _connectionMode, value);
        }

        /// <summary>
        /// 客户端配置
        /// </summary>
        public TcpInfoModel ClientConfig {
            get => _clientConfig;
            set => SetProperty(ref _clientConfig, value);
        }

        /// <summary>
        /// 服务端配置
        /// </summary>
        public TcpInfoModel ServerConfig {
            get => _serverConfig;
            set => SetProperty(ref _serverConfig, value);
        }
    }
}