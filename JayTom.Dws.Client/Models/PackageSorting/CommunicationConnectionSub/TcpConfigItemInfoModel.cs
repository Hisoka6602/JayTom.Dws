using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub {

    public class TcpConfigItemInfoModel : BasePackageSortingItemInfoModel {
        private string _ipAddress = string.Empty;
        private int _port;

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
    }
}