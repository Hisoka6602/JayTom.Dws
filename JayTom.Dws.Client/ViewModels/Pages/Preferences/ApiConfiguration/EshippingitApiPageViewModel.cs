using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration {

    public class EshippingitApiPageViewModel : SettingsPageTemplateViewModel {
        private EshippingitApiModel _eshippingitApiInfo = new();

        public EshippingitApiModel EshippingitApiInfo {
            get => _eshippingitApiInfo;
            set => SetProperty(ref _eshippingitApiInfo, value);
        }

        public EshippingitApiPageViewModel(IConfigRepository configRepository) : base(configRepository) {
        }

        public override string Identifier => "EshippingitApiParametersDialogHost";
        public override string SettingsName => "EshippingitApiParameters";

        protected override Task<bool> SaveSettingsProcess() {
            return Task.FromResult(true);
        }

        public override async void LoadedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var settingsDto = await _configRepository.FirstOrDefaultEntity<EshippingitApiDto>(SettingsName) ?? new EshippingitApiDto();
                EshippingitApiInfo = new EshippingitApiModel() {
                    Domain = settingsDto.Domain,
                    TimeOut = settingsDto.TimeOut,
                    Authorization = settingsDto.Authorization,
                    Endpoint = settingsDto.Endpoint,
                    BucketName = settingsDto.BucketName,
                };
            });
        }
    }
}