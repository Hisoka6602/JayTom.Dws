using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.Models.AppSettingModel
{

    public class SyncSettingsModel : BindableBase
    {
        private string _url = string.Empty;
        private bool _isUseAlgorithmSync;
        private bool _isUseApiSync;
        private bool _isUseImageStorageSync;
        private bool _isUseFilterSync;
        private bool _isUseContentInputSync;
        private bool _isUsePackagingSync;
        private bool _isUseOcrSync;
        private bool _isUseCloudSync;
        private bool _isUseSpaceCleaningSync;
        private bool _isUseSyncSettings;

        private ObservableCollection<SettingsSyncItemInfoModel> _packageSortingSyncItems = new()
        {
            new SettingsSyncItemInfoModel {DisplayName ="连接配置同步:",Value = "IsUseConnectionSync"},
            new SettingsSyncItemInfoModel {DisplayName ="出口配置同步:",Value = "IsUseExitSync"},
            new SettingsSyncItemInfoModel {DisplayName ="指令配置同步:",Value = "IsUseInstructionSync"},
            new SettingsSyncItemInfoModel {DisplayName ="物流配置同步:",Value = "IsUseLogisticsSync"},
            new SettingsSyncItemInfoModel {DisplayName ="分拣模式配置同步:",Value = "IsUseSortingModeSync"},
            new SettingsSyncItemInfoModel {DisplayName ="锁格配置同步:",Value = "IsUseLockerExitSync"},
            new SettingsSyncItemInfoModel {DisplayName ="叠包配置同步:",Value = "IsUseStackingSync"},
            new SettingsSyncItemInfoModel {DisplayName ="供包台模式配置同步:",Value = "IsUseSupplyCounterSync"},
        };

        public bool IsUseSyncSettings
        {
            get => _isUseSyncSettings;
            set => SetProperty(ref _isUseSyncSettings, value);
        }

        /// <summary>
        /// 同步的 URL。
        /// </summary>
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        /// <summary>
        /// 是否使用算法配置同步。
        /// </summary>
        public bool IsUseAlgorithmSync
        {
            get => _isUseAlgorithmSync;
            set => SetProperty(ref _isUseAlgorithmSync, value);
        }

        /// <summary>
        /// 是否使用 API 接口配置同步。
        /// </summary>
        public bool IsUseApiSync
        {
            get => _isUseApiSync;
            set => SetProperty(ref _isUseApiSync, value);
        }

        /// <summary>
        /// 是否使用存图配置同步。
        /// </summary>
        public bool IsUseImageStorageSync
        {
            get => _isUseImageStorageSync;
            set => SetProperty(ref _isUseImageStorageSync, value);
        }

        /// <summary>
        /// 是否使用过滤配置同步。
        /// </summary>
        public bool IsUseFilterSync
        {
            get => _isUseFilterSync;
            set => SetProperty(ref _isUseFilterSync, value);
        }

        /// <summary>
        /// 是否使用内容输入配置同步。
        /// </summary>
        public bool IsUseContentInputSync
        {
            get => _isUseContentInputSync;
            set => SetProperty(ref _isUseContentInputSync, value);
        }

        /// <summary>
        /// 是否组包配置同步。
        /// </summary>
        public bool IsUsePackagingSync
        {
            get => _isUsePackagingSync;
            set => SetProperty(ref _isUsePackagingSync, value);
        }

        /// <summary>
        /// 是否使用 OCR 配置同步。
        /// </summary>
        public bool IsUseOcrSync
        {
            get => _isUseOcrSync;
            set => SetProperty(ref _isUseOcrSync, value);
        }

        /// <summary>
        /// 是否使用云端配置同步。
        /// </summary>
        public bool IsUseCloudSync
        {
            get => _isUseCloudSync;
            set => SetProperty(ref _isUseCloudSync, value);
        }

        /// <summary>
        /// 是否使用空间清理配置同步。
        /// </summary>
        public bool IsUseSpaceCleaningSync
        {
            get => _isUseSpaceCleaningSync;
            set => SetProperty(ref _isUseSpaceCleaningSync, value);
        }

        public ObservableCollection<SettingsSyncItemInfoModel> PackageSortingSyncItems
        {
            get => _packageSortingSyncItems;
            set => SetProperty(ref _packageSortingSyncItems, value);
        }

        public class SettingsSyncItemInfoModel
        {
            public string DisplayName { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
            public bool IsChecked { get; set; }
        }
    }
}