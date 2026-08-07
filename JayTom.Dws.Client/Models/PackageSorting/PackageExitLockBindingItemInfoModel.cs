using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Models.PackageSorting
{

    public class PackageExitLockBindingItemInfoModel : BasePackageSortingItemInfoModel
    {
        private long _exitId;
        private string _exitName = string.Empty;
        private string _address = string.Empty;
        private int _length;
        private string _lockingFlag = string.Empty;
        private string _unlockingFlag = string.Empty;
        private ExitLockStatus _currentStatus;

        /// <summary>
        /// 格口Id
        /// </summary>
        public long ExitId
        {
            get => _exitId;
            set => SetProperty(ref _exitId, value);
        }

        /// <summary>
        /// 格口名称
        /// </summary>
        [DisplayName("格口名称"), MemberNotNull, ExcelInfo(Width = 4000)]
        public string ExitName
        {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 地址
        /// </summary>
        [DisplayName("地址"), MemberNotNull, ExcelInfo(Width = 4000)]
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        /// <summary>
        /// 长度
        /// </summary>
        [DisplayName("长度"), MemberNotNull, ExcelInfo(Width = 4000)]
        public int Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        /// <summary>
        /// 锁定标识
        /// </summary>
        [DisplayName("锁定标识"), MemberNotNull, ExcelInfo(Width = 4000)]
        public string LockingFlag
        {
            get => _lockingFlag;
            set => SetProperty(ref _lockingFlag, value);
        }

        /// <summary>
        /// 解锁标识
        /// </summary>
        [DisplayName("解锁标识"), MemberNotNull, ExcelInfo(Width = 4000)]
        public string UnlockingFlag
        {
            get => _unlockingFlag;
            set => SetProperty(ref _unlockingFlag, value);
        }

        /// <summary>
        /// 当前状态
        /// </summary>
        public ExitLockStatus CurrentStatus
        {
            get => _currentStatus;
            set => SetProperty(ref _currentStatus, value);
        }
    }
}