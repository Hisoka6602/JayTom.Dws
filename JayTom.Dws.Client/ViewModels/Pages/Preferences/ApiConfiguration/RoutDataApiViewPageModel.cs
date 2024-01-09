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
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration {

    public class RoutDataApiViewPageModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private RoutDataApiModel _routdataApiInfo = new();
        private bool _isSavingInProgress;
        private SnackbarMessageQueue _routDataApiMessageQueue = new(TimeSpan.FromSeconds(2));

        public RoutDataApiModel RoutDataApiInfo {
            get => _routdataApiInfo;
            set => SetProperty(ref _routdataApiInfo, value);
        }

        public RoutDataApiViewPageModel(IConfigRepository configRepository) {
            _configRepository = configRepository;
        }

        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        public SnackbarMessageQueue RoutDataApiMessageQueue {
            get => _routDataApiMessageQueue;
            set => SetProperty(ref _routDataApiMessageQueue, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var configInfoModel = await _configRepository.FirstOrDefault(w =>
                    w.ConfigName.Equals("RoutDataApiParameters"));
                if (configInfoModel is not null) {
                    var settingsDto = JsonConvert.DeserializeObject<RoutDataApiDto>(configInfoModel.Value);
                    if (settingsDto is not null) {
                        RoutDataApiInfo = new RoutDataApiModel() {
                            Url = settingsDto.Url,
                            TimeOut = settingsDto.TimeOut,
                            DeviceCode = settingsDto.DeviceCode,
                            RetryCount = settingsDto.RetryCount,
                            RetryInterval = settingsDto.RetryInterval,
                            SignKey = settingsDto.SignKey
                        };
                    }
                }
            });
        }

        public ICommand SaveSettingsCommand {
            get => new DelegateCommand<object>(SaveSettingDelegate);
        }

        private async void SaveSettingDelegate(object obj) {
            if (!IsSavingInProgress) {
                IsSavingInProgress = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "RoutDataApiParameters",
                        Value = JsonConvert.SerializeObject(new RoutDataApiDto() {
                            Url = RoutDataApiInfo.Url,
                            TimeOut = RoutDataApiInfo.TimeOut,
                            DeviceCode = RoutDataApiInfo.DeviceCode,
                            RetryCount = RoutDataApiInfo.RetryCount,
                            RetryInterval = RoutDataApiInfo.RetryInterval,
                            SignKey = RoutDataApiInfo.SignKey
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "RoutDataApiParameters"
                        });
                    }
                    IsSavingInProgress = false;
                    RoutDataApiMessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                        Languages.Language.ResourceManager.GetString("SaveFailed"))}");
                });
            }
        }
    }
}