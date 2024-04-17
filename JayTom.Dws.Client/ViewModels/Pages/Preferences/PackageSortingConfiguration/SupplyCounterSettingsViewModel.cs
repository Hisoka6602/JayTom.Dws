using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using Newtonsoft.Json;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.CommunicationsSettingsModel;
using JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    /// <summary>
    /// 供包台设置
    /// </summary>
    public class SupplyCounterSettingsViewModel : SettingsPageTemplateViewModel {
        private SupplyCounterInfoModel _supplyCounterInfo = new();

        public SupplyCounterSettingsViewModel(IConfigRepository configRepository) : base(configRepository) {
        }

        public override string Identifier => "PackageSortingSettingsDialog";
        public override string SettingsName => "SupplyCounterSettings";

        public SupplyCounterInfoModel SupplyCounterInfo {
            get => _supplyCounterInfo;
            set => SetProperty(ref _supplyCounterInfo, value);
        }

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new SupplyCounterSettingsDto {
                    SendPreSequenceNumber = SupplyCounterInfo.SendPreSequenceNumber,
                    IsUseSupplyCounterMode = SupplyCounterInfo.IsUseSupplyCounterMode,
                    WaitForVolumeInformation = SupplyCounterInfo.WaitForVolumeInformation,
                    StartPrecedingNumber = SupplyCounterInfo.StartPrecedingNumber,
                    PrecedingSignalMaxValue = SupplyCounterInfo.PrecedingSignalMaxValue,
                    IsWaitForPrecedingSignalReplyBeforeCreatingNewPackage = SupplyCounterInfo.IsWaitForPrecedingSignalReplyBeforeCreatingNewPackage,
                    IsWaitForBindingCarSignalToCompletePackage = SupplyCounterInfo.IsWaitForBindingCarSignalToCompletePackage,
                    PrecedingReplySignalTimeout = SupplyCounterInfo.PrecedingReplySignalTimeout,
                })
            });
            base.MessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(insertOrUpdate ?
                Languages.Language.ResourceManager.GetString("Success") :
                Languages.Language.ResourceManager.GetString("Failure"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var settingsDto = await _configRepository.FirstOrDefaultEntity<SupplyCounterSettingsDto>(SettingsName) ?? new SupplyCounterSettingsDto();

                SupplyCounterInfo = new SupplyCounterInfoModel() {
                    SendPreSequenceNumber = settingsDto.SendPreSequenceNumber,
                    IsUseSupplyCounterMode = settingsDto.IsUseSupplyCounterMode,
                    WaitForVolumeInformation = settingsDto.WaitForVolumeInformation,
                    StartPrecedingNumber = settingsDto.StartPrecedingNumber,
                    PrecedingSignalMaxValue = settingsDto.PrecedingSignalMaxValue,
                    IsWaitForPrecedingSignalReplyBeforeCreatingNewPackage = settingsDto.IsWaitForPrecedingSignalReplyBeforeCreatingNewPackage,
                    IsWaitForBindingCarSignalToCompletePackage = settingsDto.IsWaitForBindingCarSignalToCompletePackage,
                    PrecedingReplySignalTimeout = settingsDto.PrecedingReplySignalTimeout,
                };
            });
        }
    }
}