using System;
using Prism.Mvvm;
using Prism.Commands;
using Newtonsoft.Json;
using System.Net.Http;
using System.Windows.Input;
using Prism.Services.Dialogs;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Interface.Szjy188;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration {

    public class SzjyApiPageViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDialogService _dialogService;
        private string _barcode = string.Empty;
        private double _weight;
        private double _length;
        private double _width;
        private double _height;
        private bool _isLoggingIn;
        private bool _isUploading;
        private SzjyApiInfoModel _szjyApiInfo = new();
        private string _username = string.Empty;
        private int _uid;
        private bool _isLoginSuccessful;
        private bool _isSavingInProgress;
        private bool _isLoaded;
        private SnackbarMessageQueue _szjyApiMessageQueue = new(TimeSpan.FromSeconds(2));
        private string _nickname = string.Empty;

        public SzjyApiPageViewModel(IConfigRepository configRepository, IHttpClientFactory httpClientFactory,
            IDialogService dialogService) {
            _configRepository = configRepository;
            _httpClientFactory = httpClientFactory;
            _dialogService = dialogService;
        }

        public SzjyApiInfoModel SzjyApiInfo {
            get => _szjyApiInfo;
            set => SetProperty(ref _szjyApiInfo, value);
        }

        public SnackbarMessageQueue SzjyApiMessageQueue {
            get => _szjyApiMessageQueue;
            set => SetProperty(ref _szjyApiMessageQueue, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        /// <summary>
        /// 重量
        /// </summary>
        public double Weight {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        /// <summary>
        /// 长度
        /// </summary>
        public double Length {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        /// <summary>
        /// 宽度
        /// </summary>
        public double Width {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// 高度
        /// </summary>
        public double Height {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// 昵称
        /// </summary>
        public string Nickname {
            get => _nickname;
            set => SetProperty(ref _nickname, value);
        }

        /// <summary>
        /// uid
        /// </summary>
        public int Uid {
            get => _uid;
            set => SetProperty(ref _uid, value);
        }

        /// <summary>
        /// 是否成功登录
        /// </summary>
        public bool IsLoginSuccessful {
            get => _isLoginSuccessful;
            set => SetProperty(ref _isLoginSuccessful, value);
        }

        /// <summary>
        /// 登录中
        /// </summary>
        public bool IsLoggingIn {
            get => _isLoggingIn;
            set => SetProperty(ref _isLoggingIn, value);
        }

        /// <summary>
        /// 上传中
        /// </summary>
        public bool IsUploading {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        public ICommand SaveSettingsCommand {
            get => new DelegateCommand<object>(SaveSettingDelegate);
        }

        private async void SaveSettingDelegate(object obj) {
            if (!IsSavingInProgress) {
                IsSavingInProgress = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "SzjyApiParameters",
                        Value = JsonConvert.SerializeObject(new SzjyApiDto {
                            UserName = SzjyApiInfo.UserName,
                            Machine = SzjyApiInfo.Machine,
                            Password = SzjyApiInfo.Password,
                            TimeOut = SzjyApiInfo.TimeOut,
                            Url = SzjyApiInfo.Url,
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "SzjyApiParameters"
                        });
                    }
                    IsSavingInProgress = false;
                    SzjyApiMessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
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
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("SzjyApiParameters"));
                    if (configInfoModel is not null) {
                        var settingsDto = JsonConvert.DeserializeObject<SzjyApiDto>(configInfoModel.Value);
                        if (settingsDto is not null) {
                            SzjyApiInfo = new SzjyApiInfoModel() {
                                Url = settingsDto.Url,
                                Machine = settingsDto.Machine,
                                Password = settingsDto.Password,
                                TimeOut = settingsDto.TimeOut,
                                UserName = settingsDto.UserName
                            };
                        }
                    }
                });
            }
        }

        public ICommand LogInCommand {
            get => new DelegateCommand<object>(LogInDelegate);
        }

        private async void LogInDelegate(object obj) {
            if (!IsLoggingIn) {
                IsLoggingIn = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    IsLoginSuccessful = false;
                    //设置参数
                    var szjyApi = new SzjyApi(_httpClientFactory);
                    await szjyApi.SetParameters(new SzjyApi.ApiParameter() {
                        Machine = SzjyApiInfo.Machine,
                        Password = SzjyApiInfo.Password,
                        Url = SzjyApiInfo.Url,
                        UserName = SzjyApiInfo.UserName,
                        TimeOut = SzjyApiInfo.TimeOut,
                    });

                    //登录
                    var (key, value) = await szjyApi.LogIn(SzjyApiInfo.UserName, SzjyApiInfo.Password);
                    if (key && value is not null) {
                        if (value.Status == 0) {
                            IsLoginSuccessful = true;
                            Username = value.UserName;
                            Nickname = value.NickName;
                            Uid = value.Uid;
                            IsLoginSuccessful = true;
                        }
                        else {
                            SzjyApiMessageQueue.Enqueue($"{value.Message}");
                        }
                    }
                    else {
                        SzjyApiMessageQueue.Enqueue("连接失败!");
                    }
                    IsLoggingIn = false;
                });
            }
        }

        public ICommand UploadCommand {
            get => new DelegateCommand<object>(UploadDelegate);
        }

        private async void UploadDelegate(object obj) {
            if (!IsUploading) {
                IsUploading = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    //上传
                    var szjyApi = new SzjyApi(_httpClientFactory);
                    await szjyApi.SetParameters(new SzjyApi.ApiParameter() {
                        Machine = SzjyApiInfo.Machine,
                        Password = SzjyApiInfo.Password,
                        Url = SzjyApiInfo.Url,
                        UserName = SzjyApiInfo.UserName,
                        TimeOut = SzjyApiInfo.TimeOut,
                    });
                    var uploadResponse = await szjyApi.UploadData(Barcode, Weight, Length, Width, Height);
                    IsUploading = false;
                    //弹窗
                    _dialogService.ShowDialog("ApiTestDialog", new DialogParameters { { "UploadResponse", uploadResponse } }, null);
                });
            }
        }
    }
}