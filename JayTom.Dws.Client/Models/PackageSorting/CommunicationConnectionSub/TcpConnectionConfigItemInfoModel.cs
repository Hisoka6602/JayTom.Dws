using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub
{

    public class TcpConnectionConfigItemInfoModel : BasePackageSortingItemInfoModel
    {
        public TcpConnectionMode ConnectionMode
        {
            get;
            set => SetProperty(ref field, value);
        } = TcpConnectionMode.Client;

        /// <summary>
        /// 服务端信息
        /// </summary>
        public TcpConfigItemInfoModel? ServerParameter
        {
            get;
            set => SetProperty(ref field, value);
        } = new()
        {
            CreateTime = DateTime.Now,
            IpAddress = "127.0.0.1",
            Port = 2000,
        };

        /// <summary>
        /// 客户端信息
        /// </summary>
        public TcpConfigItemInfoModel? ClientParameter
        {
            get;
            set => SetProperty(ref field, value);
        } = new();

        /// <summary>
        /// 数据格式
        /// </summary>
        public DataFormatTypeInfoModel DataFormat { get; set; } = new();
    }
}
