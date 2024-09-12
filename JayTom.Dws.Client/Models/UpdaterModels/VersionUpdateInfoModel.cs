using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.UpdaterModels {

    public class VersionUpdateInfoModel : BindableBase {
        private string _versionNumber = string.Empty;
        private long _packageSize;
        private string _updateMessage = string.Empty;

        /// <summary>
        /// 版本号
        /// </summary>
        public string VersionNumber {
            get => _versionNumber;
            set => SetProperty(ref _versionNumber, value);
        }

        /// <summary>
        /// 更新包大小（以字节为单位）
        /// </summary>
        public long PackageSize {
            get => _packageSize;
            set => SetProperty(ref _packageSize, value);
        }

        /// <summary>
        /// 更新信息
        /// </summary>
        public string UpdateMessage {
            get => _updateMessage;
            set => SetProperty(ref _updateMessage, value);
        }
    }
}