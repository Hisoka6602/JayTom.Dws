using JayTom.Dws.Application.Configuration;
using System;
using NPOI.Util;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration
{

    public class RoutDataApiViewPageModel : SettingsPageTemplateViewModel
    {
        public RoutDataApiModel RoutDataApiInfo
        {
            get;
            set => SetProperty(ref field, value);
        } = new();

        public RoutDataApiViewPageModel(ISettingsStore settingsStore) : base(settingsStore)
        {
        }

        public override async void LoadedDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var settingsDto = await _settingsStore.GetAsync<RoutDataApiDto>(SettingsName) ?? new RoutDataApiDto();
                RoutDataApiInfo = new RoutDataApiModel()
                {
                    Url = settingsDto.Url,
                    TimeOut = settingsDto.TimeOut,
                    DeviceCode = settingsDto.DeviceCode,
                    RetryCount = settingsDto.RetryCount,
                    RetryInterval = settingsDto.RetryInterval,
                    SignKey = settingsDto.SignKey,
                    OrgCode = settingsDto.OrgCode,
                };
            });
        }

        public override string Identifier => "RoutDataApiParametersDialogHost";
        public override string SettingsName => "RoutDataApiParameters";

        protected override async Task<bool> SaveSettingsProcess()
        {
            var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,new RoutDataApiDto()
                {
                    Url = RoutDataApiInfo.Url,
                    TimeOut = RoutDataApiInfo.TimeOut,
                    DeviceCode = RoutDataApiInfo.DeviceCode,
                    RetryCount = RoutDataApiInfo.RetryCount,
                    RetryInterval = RoutDataApiInfo.RetryInterval,
                    SignKey = RoutDataApiInfo.SignKey,
                    OrgCode = RoutDataApiInfo.OrgCode,
                });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }
    }
}
