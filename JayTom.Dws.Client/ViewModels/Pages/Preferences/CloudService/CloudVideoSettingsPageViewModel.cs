using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Drawing;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using System.Threading;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Domain.Dto.AppDto;
using System.Collections.ObjectModel;
using JayTom.Dws.Domain.Dto.CloudDto;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.LogsItemModels;
using JayTom.Dws.Client.Models.AppSettingModel;
using JayTom.Dws.Client.Models.CloudSettingModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CloudService {

    public class CloudVideoSettingsPageViewModel : SettingsPageTemplateViewModel {
        private CloudVideoSettingsModel _cloudVideoSettings = new();
        private bool _isLoaded;
        private ObservableCollection<BaseLogItemModel> _logItems = new();
        private SemaphoreSlim _logSlim = new(1);

        public CloudVideoSettingsPageViewModel(IConfigRepository configRepository) : base(configRepository) {
            EventAggregator.Instance.Subscribe<CloudVideoUploadMessage>(async item => {
                if (item is { } model) {
                    try {
                        await _logSlim.WaitAsync();
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            LogItems.Insert(0, new BaseLogItemModel() {
                                CreateTime = DateTime.Now,
                                Message =
                                    $"条码:[{model.Barcode}],上传[{(model.IsSuccessful ? "成功" : "失败")}],扫码图数量:{model.ScanImageCount},全景图数量:{model.PanoramaImageCount}"
                            });
                            if (LogItems.Count > 100) {
                                LogItems.RemoveAt(LogItems.Count - 1);
                            }
                        }, DispatcherPriority.Background);
                    }
                    finally {
                        _logSlim.Release();
                    }
                }
            });
            EventAggregator.Instance.Subscribe<CloudVideoUploadRetryMessage>(async item => {
                if (item is { } model) {
                    try {
                        await _logSlim.WaitAsync();
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            LogItems.Insert(0, new BaseLogItemModel() {
                                CreateTime = DateTime.Now,
                                Message = $"条码:[{model.Barcode}],重试次数:{model.RetryCount}"
                            });
                            if (LogItems.Count > 100) {
                                LogItems.RemoveAt(LogItems.Count - 1);
                            }
                        }, DispatcherPriority.Background);
                    }
                    finally {
                        _logSlim.Release();
                    }
                }
            });
        }

        public CloudVideoSettingsModel CloudVideoSettings {
            get => _cloudVideoSettings;
            set => SetProperty(ref _cloudVideoSettings, value);
        }

        public ObservableCollection<BaseLogItemModel> LogItems {
            get => _logItems;
            set => SetProperty(ref _logItems, value);
        }

        public override string Identifier => "CloudServiceDialogHost";
        public override string SettingsName => "CloudVideoSettings";

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new CloudVideoSettingsDto() {
                    Concurrency = CloudVideoSettings.Concurrency,
                    IsAutoUploadUnsyncedData = CloudVideoSettings.IsAutoUploadUnsyncedData,
                    IsUseCloudVideoUpload = CloudVideoSettings.IsUseCloudVideoUpload,
                    LoginName = CloudVideoSettings.LoginName,
                    NodeName = CloudVideoSettings.NodeName,
                    RequestTimeout = CloudVideoSettings.RequestTimeout,
                    RetryAttempts = CloudVideoSettings.RetryAttempts,
                    WebDoMain = CloudVideoSettings.WebDoMain,
                    UploadIntervalInSeconds = CloudVideoSettings.UploadIntervalInSeconds
                })
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                var cloudVideoSettingsDto = await _configRepository.FirstOrDefaultEntity<CloudVideoSettingsDto>(SettingsName) ?? new CloudVideoSettingsDto();
                CloudVideoSettings = new CloudVideoSettingsModel {
                    Concurrency = cloudVideoSettingsDto.Concurrency,
                    IsAutoUploadUnsyncedData = cloudVideoSettingsDto.IsAutoUploadUnsyncedData,
                    IsUseCloudVideoUpload = cloudVideoSettingsDto.IsUseCloudVideoUpload,
                    LoginName = cloudVideoSettingsDto.LoginName,
                    NodeName = cloudVideoSettingsDto.NodeName,
                    RequestTimeout = cloudVideoSettingsDto.RequestTimeout,
                    RetryAttempts = cloudVideoSettingsDto.RetryAttempts,
                    WebDoMain = cloudVideoSettingsDto.WebDoMain,
                    UploadIntervalInSeconds = cloudVideoSettingsDto.UploadIntervalInSeconds
                };
            }
        }

        public ICommand ClearLogCommand => new DelegateCommand<object>(ClearLogDelegate);

        private async void ClearLogDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(LogItems.Clear);
        }
    }
}
