using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Models.OcrSettingsModel;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Domain.Repository.LocalConf;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows.Input;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class OcrSettingsViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private OcrSettingsInfoModel _ocrSettingsInfo = new();
        private SnackbarMessageQueue _ocrSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isSavingInProgress;
        private bool _isLoaded;

        public OcrSettingsViewModel(IConfigRepository configRepository) {
            _configRepository = configRepository;
        }

        public SnackbarMessageQueue OcrSettingsMessageQueue {
            get => _ocrSettingsMessageQueue;
            set => SetProperty(ref _ocrSettingsMessageQueue, value);
        }

        public OcrSettingsInfoModel OcrSettingsInfo {
            get => _ocrSettingsInfo;
            set => SetProperty(ref _ocrSettingsInfo, value);
        }

        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public ICommand SaveSettingsCommand {
            get => new DelegateCommand<object>(SaveSettingDelegate);
        }

        private async void SaveSettingDelegate(object obj) {
            if (!IsSavingInProgress) {
                IsSavingInProgress = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "OcrSettings",
                        Value = JsonConvert.SerializeObject(new OcrSettingsDto() {
                            IsShowCompartmentNumber = OcrSettingsInfo.IsShowCompartmentNumber,
                            IsShowLogisticsCompany = OcrSettingsInfo.IsShowLogisticsCompany,
                            IsShowReceiverInfo = OcrSettingsInfo.IsShowReceiverInfo,
                            IsShowRecognitionTime = OcrSettingsInfo.IsShowRecognitionTime,
                            IsShowSenderInfo = OcrSettingsInfo.IsShowSenderInfo,
                            IsUseOcr = OcrSettingsInfo.IsUseOcr
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "OcrSettings"
                        });
                    }

                    IsSavingInProgress = false;
                    OcrSettingsMessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                        Languages.Language.ResourceManager.GetString("SaveFailed"))}");
                });
            }
        }

        /// <summary>
        /// 页面加载完成
        /// </summary>
        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("OcrSettings"));

                    if (configInfoModel is not null) {
                        try {
                            var ocrSettingsDto = JsonConvert.DeserializeObject<OcrSettingsDto>(configInfoModel.Value);
                            if (ocrSettingsDto is not null) {
                                OcrSettingsInfo = new OcrSettingsInfoModel() {
                                    IsShowCompartmentNumber = ocrSettingsDto.IsShowCompartmentNumber,
                                    IsShowLogisticsCompany = ocrSettingsDto.IsShowLogisticsCompany,
                                    IsShowReceiverInfo = ocrSettingsDto.IsShowReceiverInfo,
                                    IsShowRecognitionTime = ocrSettingsDto.IsShowRecognitionTime,
                                    IsShowSenderInfo = ocrSettingsDto.IsShowSenderInfo,
                                    IsUseOcr = ocrSettingsDto.IsUseOcr
                                };
                            }
                        }
                        catch (Exception e) {
                            OcrSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("加载设置失败") ?? string.Empty}:{e.Message}");
                        }
                    }
                });
            }
        }
    }
}