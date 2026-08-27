using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using System.Collections.Generic;
using JayTom.Dws.Legacy.Contracts.Dto.BaseInfoModels;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Legacy.Contracts.Dto {

    public class StackedPackageDetectionSettingsDto {

        /// <summary>
        /// 是否监测叠包
        /// </summary>
        public bool IsStackedPackageDetection { get; set; }

        /// <summary>
        /// 通讯方式
        /// </summary>
        public CommunicationsType CommunicationType { get; set; }

        /// <summary>
        /// 串口配置
        /// </summary>
        public SerialPortSettingsInfo? SerialPortConfigInfo { get; set; }

        /// <summary>
        /// Tcp配置
        /// </summary>
        public TcpSettingsInfo? TcpConnectionConfigInfo { get; set; }

        /// <summary>
        /// 判断正则表达式
        /// </summary>
        public string RegularExpression { get; set; } = string.Empty;

        /// <summary>
        /// 判断的字符
        /// </summary>
        public string CheckerContent { get; set; } = string.Empty;

        /// <summary>
        /// 判断超时时间
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 是否自动异常口
        /// </summary>
        public bool IsAutoExceptionSorting { get; set; }
    }
}