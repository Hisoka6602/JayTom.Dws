using JayTom.Dws.Application.Configuration;
using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Models.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Legacy.Contracts.Dto.ApiDto;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration
{

    public class CaiNiaoApiPageViewModel : SettingsPageTemplateViewModel
    {
        private CaiNiaoApiModel _caiNiaoApiInfo = new();

        public CaiNiaoApiPageViewModel(ISettingsStore settingsStore, JayTom.Dws.Application.Messaging.IEventBus eventBus) : base(settingsStore, eventBus)
        {
        }

        public CaiNiaoApiModel CaiNiaoApiInfo
        {
            get => _caiNiaoApiInfo;
            set => SetProperty(ref _caiNiaoApiInfo, value);
        }

        public override async void LoadedDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var settingsDto = await _settingsStore.GetAsync<CaiNiaoApiDto>(SettingsName) ?? new CaiNiaoApiDto();
                CaiNiaoApiInfo = new CaiNiaoApiModel()
                {
                    BcrCode = settingsDto.BcrCode,
                    BcrName = settingsDto.BcrName,
                    Source = settingsDto.Source,
                    TimeOut = settingsDto.TimeOut,
                    Url = settingsDto.Url,
                    Version = settingsDto.Version
                };
            });
        }

        public override string Identifier => "CaiNiaoApiParametersDialogHost";
        public override string SettingsName => "CaiNiaoApiParameters";

        protected override async Task<bool> SaveSettingsProcess()
        {
            var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,new CaiNiaoApiDto()
                {
                    BcrCode = CaiNiaoApiInfo.BcrCode,
                    BcrName = CaiNiaoApiInfo.BcrName,
                    Source = CaiNiaoApiInfo.Source,
                    TimeOut = CaiNiaoApiInfo.TimeOut,
                    Url = CaiNiaoApiInfo.Url,
                    Version = CaiNiaoApiInfo.Version
                });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }
    }
}