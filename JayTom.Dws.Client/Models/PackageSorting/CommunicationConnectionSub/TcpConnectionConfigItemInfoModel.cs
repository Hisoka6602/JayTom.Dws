using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub {

    public class TcpConnectionConfigItemInfoModel : BasePackageSortingItemInfoModel {

        private TcpConfigItemInfoModel? _serverParameter = new() {
            CreateTime = DateTime.Now,
            IpAddress = "127.0.0.1",
            Port = 2000,
        };

        private TcpConfigItemInfoModel? _clientParameter = new();
        private TcpConnectionMode _connectionMode = TcpConnectionMode.Client;

        public TcpConnectionMode ConnectionMode {
            get => _connectionMode;
            set => SetProperty(ref _connectionMode, value);
        }

        /// <summary>
        /// 服务端信息
        /// </summary>
        public TcpConfigItemInfoModel? ServerParameter {
            get => _serverParameter;
            set => SetProperty(ref _serverParameter, value);
        }

        /// <summary>
        /// 客户端信息
        /// </summary>
        public TcpConfigItemInfoModel? ClientParameter {
            get => _clientParameter;
            set => SetProperty(ref _clientParameter, value);
        }

        /// <summary>
        /// 数据格式
        /// </summary>
        public DataFormatTypeInfoModel DataFormat { get; set; } = new();
    }
}