using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class PackageExitDefinitionItemInfoModel : BasePackageSortingItemInfoModel {
        private string _exitName = string.Empty;
        private ExitType _type = ExitType.PackageExit;
        private bool _isActive;

        /// <summary>
        /// 格口名称
        /// </summary>
        public string ExitName {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 格口类型
        /// </summary>
        public ExitType Type {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 是否生效
        /// </summary>
        public bool IsActive {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }
    }
}