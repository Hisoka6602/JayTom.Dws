using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;

namespace JayTom.Dws.Client.Models.PackageSorting.PackageExitLockModels {

    public class PackageExitLockSettingsModel : BindableBase {
        private S7ConfigModel _s7Config = new();
        private LockProtocolType _protocolType = LockProtocolType.S7;
        private bool _isUsePackageExitLock;
        private bool _isAutoExceptionSorting = true;

        /// <summary>
        /// 是否使用锁格监测
        /// </summary>
        public bool IsUsePackageExitLock {
            get => _isUsePackageExitLock;
            set => SetProperty(ref _isUsePackageExitLock, value);
        }

        /// <summary>
        /// 协议类型
        /// </summary>
        public LockProtocolType ProtocolType {
            get => _protocolType;
            set => SetProperty(ref _protocolType, value);
        }

        /// <summary>
        /// S7连接配置
        /// </summary>
        public S7ConfigModel S7Config {
            get => _s7Config;
            set => SetProperty(ref _s7Config, value);
        }

        /// <summary>
        /// 是否自动异常口
        /// </summary>
        public bool IsAutoExceptionSorting {
            get => _isAutoExceptionSorting;
            set => SetProperty(ref _isAutoExceptionSorting, value);
        }
    }
}