using System;
using System.IO;
using Prism.Mvvm;
using Prism.Commands;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms;
using JayTom.Dws.Domain.Dto;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Windows.Media.Imaging;
using JayTom.Dws.Domain.Dto.AppDto;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.AppSettingModel;
using JayTom.Dws.Client.Models.OcrSettingsModel;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings {
    public class OtherSettingsViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private OtherSettingsModel _otherSettingsInfo = new();
        private SnackbarMessageQueue _otherSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private ImageSource? _icon;
        private bool _isSavingInProgress;
        private string _fileName = string.Empty;
        private bool _isLoaded;

        public OtherSettingsViewModel(IConfigRepository configRepository) {
            _configRepository = configRepository;
        }

        public OtherSettingsModel OtherSettingsInfo {
            get => _otherSettingsInfo;
            set => SetProperty(ref _otherSettingsInfo, value);
        }

        public SnackbarMessageQueue OtherSettingsMessageQueue {
            get => _otherSettingsMessageQueue;
            set => SetProperty(ref _otherSettingsMessageQueue, value);
        }

        public string FileName {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        /// <summary>
        /// 图标
        /// </summary>
        public ImageSource? Icon {
            get => _icon;
            set => SetProperty(ref _icon, value);
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
                    try {
                        if (!Directory.Exists($"{AppContext.BaseDirectory}Logo")) {
                            //创建图片路径
                            Directory.CreateDirectory($"{AppContext.BaseDirectory}Logo");
                        }
                        var dest = string.Empty;
                        if (!string.IsNullOrEmpty(FileName)) {
                            dest = $"{AppContext.BaseDirectory}Logo\\{new FileInfo(FileName).Name}";
                            File.Copy(FileName, dest, true);
                        }

                        var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                            ConfigName = "OtherSettings",
                            Value = JsonConvert.SerializeObject(new OtherSettingsDto() {
                                IsAutoMaximize = OtherSettingsInfo.IsAutoMaximize,
                                IsAutoStart = OtherSettingsInfo.IsAutoStart,
                                ProgramTitle = OtherSettingsInfo.ProgramTitle,
                                ProgramLogoPath = dest
                            })
                        });
                        if (insertOrUpdate) {
                            EventAggregator.Instance.Publish(new SettingsChangedEvent {
                                SettingsName = "OtherSettings"
                            });
                            OtherSettingsMessageQueue.Enqueue($"保存成功!");
                        }
                        else {
                            OtherSettingsMessageQueue.Enqueue($"保存失败!");
                        }
                    }
                    catch (Exception e) {
                        OtherSettingsMessageQueue.Enqueue(e.Message);
                    }
                    finally {
                        IsSavingInProgress = false;
                    }
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
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("OtherSettings"));
                    if (configInfoModel is not null) {
                        try {
                            var otherSettingsDto = JsonConvert.DeserializeObject<OtherSettingsDto>(configInfoModel.Value);
                            if (otherSettingsDto is not null) {
                                OtherSettingsInfo = new OtherSettingsModel() {
                                    IsAutoMaximize = otherSettingsDto.IsAutoMaximize,
                                    IsAutoStart = otherSettingsDto.IsAutoStart,
                                    ProgramLogoPath = otherSettingsDto.ProgramLogoPath,
                                    ProgramTitle = otherSettingsDto.ProgramTitle
                                };
                            }
                            //检查图片是否存在
                            //加载图片
                            if (File.Exists(OtherSettingsInfo.ProgramLogoPath)) {
                                Icon = CreateBitmapImage(new Uri(OtherSettingsInfo.ProgramLogoPath), 30, 30);
                            }
                        }
                        catch (Exception e) {
                            OtherSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("加载设置失败") ?? string.Empty}:{e.Message}");
                        }
                    }
                });
            }
        }

        public ICommand LoadImageCommand {
            get => new DelegateCommand<object>(LoadImageDelegate);
        }

        private async void LoadImageDelegate(object obj) {
            var openFileDialog = new OpenFileDialog() {
                Filter = @"*.PNG|*.PNG",
                InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "请选择图像文件",
                RestoreDirectory = true,
            };
            var showDialog = openFileDialog.ShowDialog();
            if (showDialog == DialogResult.OK) {
                if (!string.IsNullOrEmpty(openFileDialog.FileName)) {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        Icon = CreateBitmapImage(new Uri(openFileDialog.FileName), 30, 30);
                        FileName = openFileDialog.FileName;
                    });
                }
            }
        }

        public BitmapImage CreateBitmapImage(Uri uri, int width, int height) {
            try {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = uri;
                image.DecodePixelHeight = height;
                image.DecodePixelWidth = width;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch {
                // ignored
            }

            return null;
        }
    }
}