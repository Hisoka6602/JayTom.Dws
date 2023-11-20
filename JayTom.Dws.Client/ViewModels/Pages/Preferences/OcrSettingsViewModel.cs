using System;
using Prism.Mvvm;
using Prism.Commands;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.OcrSettingsModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class OcrSettingsViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private readonly IOcr _ocr;
        private readonly IDeviceService _deviceService;
        private OcrSettingsInfoModel _ocrSettingsInfo = new();
        private SnackbarMessageQueue _ocrSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isSavingInProgress;
        private bool _isLoaded;

        public OcrSettingsViewModel(IConfigRepository configRepository, IOcr ocr,
            IDeviceService deviceService) {
            _configRepository = configRepository;
            _ocr = ocr;
            _deviceService = deviceService;
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
                    if (_deviceService.RunningStatus) {
                        IsSavingInProgress = false;
                        OcrSettingsMessageQueue.Enqueue($"设备工作中,无法设置");
                        return;
                    }

                    //即时设置Ocr文件
                    var dictionary = new Dictionary<string, object>()
                    {
                        {"three_segment_code", OcrSettingsInfo.IsThreeSegmentCode},
                        {"recipient_name", OcrSettingsInfo.IsShowReceiverInfo},
                        {"recipient_phone", OcrSettingsInfo.IsShowReceiverInfo},
                        {"recipient_addr", OcrSettingsInfo.IsShowReceiverInfo},
                        {"sender_name", OcrSettingsInfo.IsShowSenderInfo},
                        {"sender_phone", OcrSettingsInfo.IsShowSenderInfo},
                        {"sender_addr", OcrSettingsInfo.IsShowSenderInfo},
                    };
                    await _ocr.SetOcrParameters(dictionary);
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "OcrSettings",
                        Value = JsonConvert.SerializeObject(new OcrSettingsDto() {
                            IsThreeSegmentCode = OcrSettingsInfo.IsThreeSegmentCode,
                            IsShowReceiverInfo = OcrSettingsInfo.IsShowReceiverInfo,
                            IsShowRecognitionTime = OcrSettingsInfo.IsShowRecognitionTime,
                            IsShowSenderInfo = OcrSettingsInfo.IsShowSenderInfo,
                            IsUseOcr = OcrSettingsInfo.IsUseOcr,
                            RecognitionTimeout = OcrSettingsInfo.RecognitionTimeout,
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
                                    IsShowReceiverInfo = ocrSettingsDto.IsShowReceiverInfo,
                                    IsShowRecognitionTime = ocrSettingsDto.IsShowRecognitionTime,
                                    IsShowSenderInfo = ocrSettingsDto.IsShowSenderInfo,
                                    IsUseOcr = ocrSettingsDto.IsUseOcr,
                                    IsThreeSegmentCode = ocrSettingsDto.IsThreeSegmentCode,
                                    RecognitionTimeout = ocrSettingsDto.RecognitionTimeout,
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