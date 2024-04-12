using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Drawing;
using System.IO.Ports;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Domain.Dto.AppDto;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.AppSettingModel;
using JayTom.Dws.Client.Models.CommunicationsSettingsModel;
using JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings {

    public class SyncSettingsViewModel : SettingsPageTemplateViewModel {
        private SyncSettingsModel _syncSettingsInfo = new();
        private bool _isConnecting;

        public SyncSettingsModel SyncSettingsInfo {
            get => _syncSettingsInfo;
            set => SetProperty(ref _syncSettingsInfo, value);
        }

        public bool IsConnecting {
            get => _isConnecting;
            set => SetProperty(ref _isConnecting, value);
        }

        public override async void LoadedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var syncSettingsDto = await _configRepository.FirstOrDefaultEntity<SyncSettingsDto>(SettingsName) ?? new SyncSettingsDto();
                SyncSettingsInfo = new SyncSettingsModel() {
                    Url = syncSettingsDto.Url,
                    IsUseAlgorithmSync = syncSettingsDto.IsUseAlgorithmSync,
                    IsUseApiSync = syncSettingsDto.IsUseApiSync,
                    IsUseImageStorageSync = syncSettingsDto.IsUseImageStorageSync,
                    IsUseFilterSync = syncSettingsDto.IsUseFilterSync,
                    IsUseContentInputSync = syncSettingsDto.IsUseContentInputSync,
                    IsUseConnectionSync = syncSettingsDto.IsUseConnectionSync,
                    IsUseExitSync = syncSettingsDto.IsUseExitSync,
                    IsUseInstructionSync = syncSettingsDto.IsUseInstructionSync,
                    IsUseLogisticsSync = syncSettingsDto.IsUseLogisticsSync,
                    IsUseSortingModeSync = syncSettingsDto.IsUseSortingModeSync,
                    IsUseLockerExitSync = syncSettingsDto.IsUseLockerExitSync,
                    IsUseStackingSync = syncSettingsDto.IsUseStackingSync,
                    IsUsePackagingSync = syncSettingsDto.IsUsePackagingSync,
                    IsUseOcrSync = syncSettingsDto.IsUseOcrSync,
                    IsUseCloudSync = syncSettingsDto.IsUseCloudSync,
                    IsUseSpaceCleaningSync = syncSettingsDto.IsUseSpaceCleaningSync,
                    IsUseSyncSettings = syncSettingsDto.IsUseSyncSettings
                };
            });
        }

        public override string Identifier => "SyncSettingsDialogHost";

        public override string SettingsName => "SyncSettingsSettings";

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new SyncSettingsDto() {
                    Url = SyncSettingsInfo.Url,
                    IsUseAlgorithmSync = SyncSettingsInfo.IsUseAlgorithmSync,
                    IsUseApiSync = SyncSettingsInfo.IsUseApiSync,
                    IsUseImageStorageSync = SyncSettingsInfo.IsUseImageStorageSync,
                    IsUseFilterSync = SyncSettingsInfo.IsUseFilterSync,
                    IsUseContentInputSync = SyncSettingsInfo.IsUseContentInputSync,
                    IsUseConnectionSync = SyncSettingsInfo.IsUseConnectionSync,
                    IsUseExitSync = SyncSettingsInfo.IsUseExitSync,
                    IsUseInstructionSync = SyncSettingsInfo.IsUseInstructionSync,
                    IsUseLogisticsSync = SyncSettingsInfo.IsUseLogisticsSync,
                    IsUseSortingModeSync = SyncSettingsInfo.IsUseSortingModeSync,
                    IsUseLockerExitSync = SyncSettingsInfo.IsUseLockerExitSync,
                    IsUseStackingSync = SyncSettingsInfo.IsUseStackingSync,
                    IsUsePackagingSync = SyncSettingsInfo.IsUsePackagingSync,
                    IsUseOcrSync = SyncSettingsInfo.IsUseOcrSync,
                    IsUseCloudSync = SyncSettingsInfo.IsUseCloudSync,
                    IsUseSpaceCleaningSync = SyncSettingsInfo.IsUseSpaceCleaningSync,
                    IsUseSyncSettings = SyncSettingsInfo.IsUseSyncSettings
                })
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public SyncSettingsViewModel(IConfigRepository configRepository) : base(configRepository) {
        }

        /// <summary>
        /// 连接
        /// </summary>
        public ICommand ConnectCommand => new DelegateCommand<object>(ConnectDelegate);

        private void ConnectDelegate(object obj) {
        }
    }
}