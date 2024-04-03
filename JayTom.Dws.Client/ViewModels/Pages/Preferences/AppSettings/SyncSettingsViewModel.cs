using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.IO.Ports;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.CommunicationsSettingsModel;
using JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings {

    public class SyncSettingsViewModel : SettingsPageTemplateViewModel {

        public override async void LoadedDelegate(object obj) {
        }

        public override string Identifier => "SyncSettingsDialogHost";

        public override string SettingsName => "SyncSettingsSettings";

        protected override async Task<bool> SaveSettingsProcess() {
            await Task.Delay(1000);
            base.MessageQueue.Enqueue("保存成功");
            return true;
        }

        public SyncSettingsViewModel(IConfigRepository configRepository) : base(configRepository) {
        }
    }
}