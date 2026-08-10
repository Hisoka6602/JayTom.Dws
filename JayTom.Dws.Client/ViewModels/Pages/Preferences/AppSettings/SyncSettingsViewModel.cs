using JayTom.Dws.Application.Configuration;
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

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings
{

    public class SyncSettingsViewModel : SettingsPageTemplateViewModel
    {
        private SyncSettingsModel _syncSettingsInfo = new();
        private bool _isConnecting;

        public SyncSettingsModel SyncSettingsInfo
        {
            get => _syncSettingsInfo;
            set => SetProperty(ref _syncSettingsInfo, value);
        }

        public bool IsConnecting
        {
            get => _isConnecting;
            set => SetProperty(ref _isConnecting, value);
        }

        public override async void LoadedDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var syncSettingsDto = await _settingsStore.GetAsync<SyncSettingsDto>(SettingsName) ?? new SyncSettingsDto();
                SyncSettingsInfo = new SyncSettingsModel()
                {
                    Url = syncSettingsDto.Url,
                    IsUseAlgorithmSync = syncSettingsDto.IsUseAlgorithmSync,
                    IsUseApiSync = syncSettingsDto.IsUseApiSync,
                    IsUseImageStorageSync = syncSettingsDto.IsUseImageStorageSync,
                    IsUseFilterSync = syncSettingsDto.IsUseFilterSync,
                    IsUseContentInputSync = syncSettingsDto.IsUseContentInputSync,
                    IsUsePackagingSync = syncSettingsDto.IsUsePackagingSync,
                    IsUseOcrSync = syncSettingsDto.IsUseOcrSync,
                    IsUseCloudSync = syncSettingsDto.IsUseCloudSync,
                    IsUseSpaceCleaningSync = syncSettingsDto.IsUseSpaceCleaningSync,
                    IsUseSyncSettings = syncSettingsDto.IsUseSyncSettings
                };
                foreach (var model in SyncSettingsInfo.PackageSortingSyncItems)
                {
                    switch (model.Value)
                    {
                        case "IsUseConnectionSync" when syncSettingsDto.IsUseConnectionSync:
                        case "IsUseExitSync" when syncSettingsDto.IsUseExitSync:
                        case "IsUseInstructionSync" when syncSettingsDto.IsUseInstructionSync:
                        case "IsUseLogisticsSync" when syncSettingsDto.IsUseLogisticsSync:
                        case "IsUseSortingModeSync" when syncSettingsDto.IsUseSortingModeSync:
                        case "IsUseLockerExitSync" when syncSettingsDto.IsUseLockerExitSync:
                        case "IsUseStackingSync" when syncSettingsDto.IsUseStackingSync:
                        case "IsUseSupplyCounterSync" when syncSettingsDto.IsUseSupplyCounterSync:
                            model.IsChecked = true;
                            break;
                    }
                }
            });
        }

        public override string Identifier => "SyncSettingsDialogHost";

        public override string SettingsName => "SyncSettingsSettings";

        protected override async Task<bool> SaveSettingsProcess()
        {
            var syncSettingsDto = new SyncSettingsDto()
            {
                Url = SyncSettingsInfo.Url,
                IsUseAlgorithmSync = SyncSettingsInfo.IsUseAlgorithmSync,
                IsUseApiSync = SyncSettingsInfo.IsUseApiSync,
                IsUseImageStorageSync = SyncSettingsInfo.IsUseImageStorageSync,
                IsUseFilterSync = SyncSettingsInfo.IsUseFilterSync,
                IsUseContentInputSync = SyncSettingsInfo.IsUseContentInputSync,
                IsUsePackagingSync = SyncSettingsInfo.IsUsePackagingSync,
                IsUseOcrSync = SyncSettingsInfo.IsUseOcrSync,
                IsUseCloudSync = SyncSettingsInfo.IsUseCloudSync,
                IsUseSpaceCleaningSync = SyncSettingsInfo.IsUseSpaceCleaningSync,
                IsUseSyncSettings = SyncSettingsInfo.IsUseSyncSettings
            };
            foreach (var model in SyncSettingsInfo.PackageSortingSyncItems)
            {
                switch (model.Value)
                {
                    case "IsUseConnectionSync":
                        syncSettingsDto.IsUseConnectionSync = model.IsChecked;
                        break;

                    case "IsUseExitSync":
                        syncSettingsDto.IsUseExitSync = model.IsChecked;
                        break;

                    case "IsUseInstructionSync":
                        syncSettingsDto.IsUseInstructionSync = model.IsChecked;
                        break;

                    case "IsUseLogisticsSync":
                        syncSettingsDto.IsUseLogisticsSync = model.IsChecked;
                        break;

                    case "IsUseSortingModeSync":
                        syncSettingsDto.IsUseSortingModeSync = model.IsChecked;
                        break;

                    case "IsUseLockerExitSync":
                        syncSettingsDto.IsUseLockerExitSync = model.IsChecked;
                        break;

                    case "IsUseStackingSync":
                        syncSettingsDto.IsUseStackingSync = model.IsChecked;
                        break;

                    case "IsUseSupplyCounterSync":
                        syncSettingsDto.IsUseSupplyCounterSync = model.IsChecked;
                        break;
                }
            }
            var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,syncSettingsDto);
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public SyncSettingsViewModel(ISettingsStore settingsStore) : base(settingsStore)
        {
        }

        /// <summary>
        /// 连接
        /// </summary>
        public ICommand ConnectCommand => new DelegateCommand<object>(ConnectDelegate);

        private void ConnectDelegate(object obj)
        {
        }
    }
}