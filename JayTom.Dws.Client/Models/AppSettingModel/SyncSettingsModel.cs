using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.AppSettingModel {

    public class SyncSettingsModel : BindableBase {
        private string _url = string.Empty;
        private bool _isUseAlgorithmSync;
        private bool _isUseApiSync;
        private bool _isUseImageStorageSync;
        private bool _isUseFilterSync;
        private bool _isUseContentInputSync;
        private bool _isUseConnectionSync;
        private bool _isUseExitSync;
        private bool _isUseInstructionSync;
        private bool _isUseLogisticsSync;
        private bool _isUseSortingModeSync;
        private bool _isUseLockerExitSync;
        private bool _isUseStackingSync;
        private bool _isUsePackagingSync;
        private bool _isUseOcrSync;
        private bool _isUseCloudSync;
        private bool _isUseSpaceCleaningSync;
        private bool _isUseSyncSettings;

        public bool IsUseSyncSettings {
            get => _isUseSyncSettings;
            set => SetProperty(ref _isUseSyncSettings, value);
        }

        /// <summary>
        /// 同步的 URL。
        /// </summary>
        public string Url {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        /// <summary>
        /// 是否使用算法配置同步。
        /// </summary>
        public bool IsUseAlgorithmSync {
            get => _isUseAlgorithmSync;
            set => SetProperty(ref _isUseAlgorithmSync, value);
        }

        /// <summary>
        /// 是否使用 API 接口配置同步。
        /// </summary>
        public bool IsUseApiSync {
            get => _isUseApiSync;
            set => SetProperty(ref _isUseApiSync, value);
        }

        /// <summary>
        /// 是否使用存图配置同步。
        /// </summary>
        public bool IsUseImageStorageSync {
            get => _isUseImageStorageSync;
            set => SetProperty(ref _isUseImageStorageSync, value);
        }

        /// <summary>
        /// 是否使用过滤配置同步。
        /// </summary>
        public bool IsUseFilterSync {
            get => _isUseFilterSync;
            set => SetProperty(ref _isUseFilterSync, value);
        }

        /// <summary>
        /// 是否使用内容输入配置同步。
        /// </summary>
        public bool IsUseContentInputSync {
            get => _isUseContentInputSync;
            set => SetProperty(ref _isUseContentInputSync, value);
        }

        /// <summary>
        /// 是否使用连接配置同步。
        /// </summary>
        public bool IsUseConnectionSync {
            get => _isUseConnectionSync;
            set => SetProperty(ref _isUseConnectionSync, value);
        }

        /// <summary>
        /// 是否使用出口配置同步。
        /// </summary>
        public bool IsUseExitSync {
            get => _isUseExitSync;
            set => SetProperty(ref _isUseExitSync, value);
        }

        /// <summary>
        /// 是否使用指令配置同步。
        /// </summary>
        public bool IsUseInstructionSync {
            get => _isUseInstructionSync;
            set => SetProperty(ref _isUseInstructionSync, value);
        }

        /// <summary>
        /// 是否使用物流配置同步。
        /// </summary>
        public bool IsUseLogisticsSync {
            get => _isUseLogisticsSync;
            set => SetProperty(ref _isUseLogisticsSync, value);
        }

        /// <summary>
        /// 是否使用分拣模式配置同步。
        /// </summary>
        public bool IsUseSortingModeSync {
            get => _isUseSortingModeSync;
            set => SetProperty(ref _isUseSortingModeSync, value);
        }

        /// <summary>
        /// 是否使用锁格配置同步。
        /// </summary>
        public bool IsUseLockerExitSync {
            get => _isUseLockerExitSync;
            set => SetProperty(ref _isUseLockerExitSync, value);
        }

        /// <summary>
        /// 是否使用叠包配置同步。
        /// </summary>
        public bool IsUseStackingSync {
            get => _isUseStackingSync;
            set => SetProperty(ref _isUseStackingSync, value);
        }

        /// <summary>
        /// 是否组包配置同步。
        /// </summary>
        public bool IsUsePackagingSync {
            get => _isUsePackagingSync;
            set => SetProperty(ref _isUsePackagingSync, value);
        }

        /// <summary>
        /// 是否使用 OCR 配置同步。
        /// </summary>
        public bool IsUseOcrSync {
            get => _isUseOcrSync;
            set => SetProperty(ref _isUseOcrSync, value);
        }

        /// <summary>
        /// 是否使用云端配置同步。
        /// </summary>
        public bool IsUseCloudSync {
            get => _isUseCloudSync;
            set => SetProperty(ref _isUseCloudSync, value);
        }

        /// <summary>
        /// 是否使用空间清理配置同步。
        /// </summary>
        public bool IsUseSpaceCleaningSync {
            get => _isUseSpaceCleaningSync;
            set => SetProperty(ref _isUseSpaceCleaningSync, value);
        }
    }
}