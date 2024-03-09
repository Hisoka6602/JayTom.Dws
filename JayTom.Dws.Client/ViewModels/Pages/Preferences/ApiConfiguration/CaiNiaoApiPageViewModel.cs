using System;
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

    public class CaiNiaoApiPageViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private CaiNiaoApiModel _caiNiaoApiInfo = new();
        private bool _isSavingInProgress;
        private SnackbarMessageQueue _caiNiaoApiMessageQueue = new(TimeSpan.FromSeconds(2));

        public CaiNiaoApiPageViewModel(IConfigRepository configRepository) {
            _configRepository = configRepository;
        }

        public CaiNiaoApiModel CaiNiaoApiInfo {
            get => _caiNiaoApiInfo;
            set => SetProperty(ref _caiNiaoApiInfo, value);
        }

        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        public SnackbarMessageQueue CaiNiaoApiMessageQueue {
            get => _caiNiaoApiMessageQueue;
            set => SetProperty(ref _caiNiaoApiMessageQueue, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var configInfoModel = await _configRepository.FirstOrDefault(w =>
                    w.ConfigName.Equals("CaiNiaoApiParameters"));
                if (configInfoModel is not null) {
                    var settingsDto = JsonConvert.DeserializeObject<CaiNiaoApiDto>(configInfoModel.Value);
                    if (settingsDto is not null) {
                        CaiNiaoApiInfo = new CaiNiaoApiModel() {
                            BcrCode = settingsDto.BcrCode,
                            BcrName = settingsDto.BcrName,
                            Source = settingsDto.Source,
                            TimeOut = settingsDto.TimeOut,
                            Url = settingsDto.Url,
                            Version = settingsDto.Version
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
                        ConfigName = "CaiNiaoApiParameters",
                        Value = JsonConvert.SerializeObject(new CaiNiaoApiModel() {
                            BcrCode = CaiNiaoApiInfo.BcrCode,
                            BcrName = CaiNiaoApiInfo.BcrName,
                            Source = CaiNiaoApiInfo.Source,
                            TimeOut = CaiNiaoApiInfo.TimeOut,
                            Url = CaiNiaoApiInfo.Url,
                            Version = CaiNiaoApiInfo.Version
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "CaiNiaoApiParameters"
                        });
                    }
                    IsSavingInProgress = false;
                    CaiNiaoApiMessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                        Languages.Language.ResourceManager.GetString("SaveFailed"))}");
                });
            }
        }
    }
}