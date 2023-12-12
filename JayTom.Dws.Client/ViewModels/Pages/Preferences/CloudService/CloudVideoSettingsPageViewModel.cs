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
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Domain.Dto.AppDto;
using System.Collections.ObjectModel;
using JayTom.Dws.Domain.Dto.CloudDto;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.LogsItemModels;
using JayTom.Dws.Client.Models.AppSettingModel;
using JayTom.Dws.Client.Models.CloudSettingModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CloudService {

    public class CloudVideoSettingsPageViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private CloudVideoSettingsModel _cloudVideoSettings = new();
        private bool _isSavingInProgress;
        private bool _isLoaded;
        private SnackbarMessageQueue _cloudVideoSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private ObservableCollection<BaseLogItemModel> _logItems = new();
        private SemaphoreSlim _logSlim = new(1);

        public CloudVideoSettingsPageViewModel(IConfigRepository configRepository) {
            _configRepository = configRepository;

            EventAggregator.Instance.Subscribe<CloudVideoUploadMessage>(async item => {
                if (item is CloudVideoUploadMessage model) {
                    try {
                        await _logSlim.WaitAsync();
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                            LogItems.Insert(0, new BaseLogItemModel() {
                                CreateTime = DateTime.Now,
                                Message = $"条码:[{model.Barcode}],上传[{(model.IsSuccessful ? "成功" : "失败")}],扫码图数量:{model.ScanImageCount},全景图数量:{model.PanoramaImageCount}"
                            });
                            if (LogItems.Count > 100) {
                                LogItems.RemoveAt(LogItems.Count - 1);
                            }
                        });
                    }
                    finally {
                        _logSlim.Release();
                    }
                }
            });
            EventAggregator.Instance.Subscribe<CloudVideoUploadRetryMessage>(async item => {
                if (item is CloudVideoUploadRetryMessage model) {
                    try {
                        await _logSlim.WaitAsync();
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                            LogItems.Insert(0, new BaseLogItemModel() {
                                CreateTime = DateTime.Now,
                                Message = $"条码:[{model.Barcode}],重试次数:{model.RetryCount}"
                            });
                            if (LogItems.Count > 100) {
                                LogItems.RemoveAt(LogItems.Count - 1);
                            }
                        });
                    }
                    finally {
                        _logSlim.Release();
                    }
                }
            });
        }

        public SnackbarMessageQueue CloudVideoSettingsMessageQueue {
            get => _cloudVideoSettingsMessageQueue;
            set => SetProperty(ref _cloudVideoSettingsMessageQueue, value);
        }

        public CloudVideoSettingsModel CloudVideoSettings {
            get => _cloudVideoSettings;
            set => SetProperty(ref _cloudVideoSettings, value);
        }

        public ObservableCollection<BaseLogItemModel> LogItems {
            get => _logItems;
            set => SetProperty(ref _logItems, value);
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
                        ConfigName = "CloudVideoSettings",
                        Value = JsonConvert.SerializeObject(new CloudVideoSettingsDto() {
                            Concurrency = CloudVideoSettings.Concurrency,
                            IsAutoUploadUnsyncedData = CloudVideoSettings.IsAutoUploadUnsyncedData,
                            IsUseCloudVideoUpload = CloudVideoSettings.IsUseCloudVideoUpload,
                            LoginName = CloudVideoSettings.LoginName,
                            NodeName = CloudVideoSettings.NodeName,
                            RequestTimeout = CloudVideoSettings.RequestTimeout,
                            RetryAttempts = CloudVideoSettings.RetryAttempts,
                            Url = CloudVideoSettings.Url,
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "CloudVideoSettings"
                        });
                    }

                    IsSavingInProgress = false;
                    CloudVideoSettingsMessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
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
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("CloudVideoSettings"));
                    if (configInfoModel is not null) {
                        try {
                            var cloudVideoSettingsDto = JsonConvert.DeserializeObject<CloudVideoSettingsDto>(configInfoModel.Value);
                            if (cloudVideoSettingsDto is not null) {
                                CloudVideoSettings = new CloudVideoSettingsModel() {
                                    Concurrency = cloudVideoSettingsDto.Concurrency,
                                    IsAutoUploadUnsyncedData = cloudVideoSettingsDto.IsAutoUploadUnsyncedData,
                                    IsUseCloudVideoUpload = cloudVideoSettingsDto.IsUseCloudVideoUpload,
                                    LoginName = cloudVideoSettingsDto.LoginName,
                                    NodeName = cloudVideoSettingsDto.NodeName,
                                    RequestTimeout = cloudVideoSettingsDto.RequestTimeout,
                                    RetryAttempts = cloudVideoSettingsDto.RetryAttempts,
                                    Url = cloudVideoSettingsDto.Url,
                                };
                            }
                        }
                        catch (Exception e) {
                            CloudVideoSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("加载设置失败") ?? string.Empty}:{e.Message}");
                        }
                    }
                });
            }
        }

        public ICommand ClearLogCommand {
            get => new DelegateCommand<object>(ClearLogDelegate);
        }

        private async void ClearLogDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                LogItems.Clear();
            });
        }
    }
}