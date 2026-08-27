using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;
using System.ComponentModel.DataAnnotations;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Models.PackageSorting
{

    public class PackageExitDefinitionItemInfoModel : BasePackageSortingItemInfoModel
    {
        private string _exitName = string.Empty;
        private ExitType _type = ExitType.PackageExit;
        private bool _isActive = true;
        private long _communicationConnectionId;
        private string? _communicationConnectionName;
        private string _mainExitName = string.Empty;
        private long _pid;

        /// <summary>
        /// 连接Id
        /// </summary>
        public long CommunicationConnectionId
        {
            get => _communicationConnectionId;
            set => SetProperty(ref _communicationConnectionId, value);
        }

        public long Pid
        {
            get => _pid;
            set => SetProperty(ref _pid, value);
        }

        /// <summary>
        /// 连接名称
        /// </summary>
        [DisplayName("连接名称"), MemberNotNull, ExcelInfo(Width = 4000)]
        public string? CommunicationConnectionName
        {
            get => _communicationConnectionName;
            set => SetProperty(ref _communicationConnectionName, value);
        }

        /// <summary>
        /// 格口名称
        /// </summary>
        [DisplayName("格口名称"), MemberNotNull, Key, ExcelInfo(Width = 5000)]
        public string ExitName
        {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 格口类型
        /// </summary>
        [DisplayName("格口类型(0=包裹出口、1=异常出口)"), MemberNotNull, ExcelInfo(Width = 6000, IsEnumToInt = true)]
        public ExitType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 格口名称
        /// </summary>
        [DisplayName("主格口"), MemberNotNull, ExcelInfo(Width = 5000)]
        public string MainExitName
        {
            get => _mainExitName;
            set => SetProperty(ref _mainExitName, value);
        }

        /// <summary>
        /// 是否生效
        /// </summary>
        [DisplayName("是否生效(0=不生效、1=生效)"), MemberNotNull, ExcelInfo(Width = 6000, IsBooleanToInt = true)]
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }
    }
}