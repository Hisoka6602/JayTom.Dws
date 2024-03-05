using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Domain.Dto {

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
    }
}