using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.PackageExitLockDto {

    public class PackageExitLockSettingsDto {

        /// <summary>
        /// 是否使用锁格监测
        /// </summary>
        public bool IsUsePackageExitLock { get; set; }

        /// <summary>
        /// 协议类型
        /// </summary>
        public LockProtocolType ProtocolType { get; set; } = LockProtocolType.S7;

        /// <summary>
        /// S7连接配置
        /// </summary>
        public S7ConfigDto S7Config { get; set; } = new();

        /// <summary>
        /// 是否自动异常口
        /// </summary>
        public bool IsAutoExceptionSorting { get; set; } = true;
    }

    public enum LockProtocolType {
        S7,
    }

    public class S7ConfigDto {

        /// <summary>
        /// IP 地址
        /// </summary>
        public string Ip { get; set; } = string.Empty;

        /// <summary>
        /// 数据库
        /// </summary>
        public int Db { get; set; }

        /// <summary>
        /// 机架号
        /// </summary>
        public int Rack { get; set; }

        /// <summary>
        /// 插槽号
        /// </summary>
        public int Slot { get; set; }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int Timeout { get; set; }
    }
}