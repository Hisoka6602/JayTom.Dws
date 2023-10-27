using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;
using System.ComponentModel.DataAnnotations;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class PackageExitDefinitionItemInfoModel : BasePackageSortingItemInfoModel {
        private string _exitName = string.Empty;
        private ExitType _type = ExitType.PackageExit;
        private bool _isActive;

        /// <summary>
        /// 格口名称
        /// </summary>
        [DisplayName("格口名称"), MemberNotNull, Key, ExcelInfo(Width = 5000)]
        public string ExitName {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 格口类型
        /// </summary>
        [DisplayName("格口类型(0=包裹出口、1=异常出口)"), MemberNotNull, ExcelInfo(Width = 6000, IsEnumToInt = true)]
        public ExitType Type {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 是否生效
        /// </summary>
        [DisplayName("是否生效(0=不生效、1=生效)"), MemberNotNull, ExcelInfo(Width = 6000, IsBooleanToInt = true)]
        public bool IsActive {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }
    }
}