using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Drawing;
using Prism.Commands;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Forms;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Brush = System.Drawing.Brush;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.PluginInterface.Utils;
using Color = System.Windows.Media.Color;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ImageSettingModels;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class SaveImageSettingsPageViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private bool _isUseWatermark;
        private string _watermarkText = "测试水印";
        private System.Windows.Media.Color _watermarkColor = Color.FromRgb(System.Drawing.Color.DodgerBlue.R, System.Drawing.Color.DodgerBlue.G, System.Drawing.Color.DodgerBlue.B);
        private int _watermarkFontSize = 10;
        private WatermarkPosition _watermarkPosition = WatermarkPosition.TopLeft;
        private ImageSource? _originalImage = new BitmapImage(new Uri("../../../Image/14.jpg", UriKind.Relative));
        private ImageSource? _imageSource;

        private ObservableCollection<ItemBaseTemplateModel> _watermarkItems = new()
        {
            new ItemBaseTemplateModel()
            {
                Id = 0,
                Content = "{BarCode}",
                Type = 1,
                ApplicationType = ItemApplicationType.Watermark
            },
            new ItemBaseTemplateModel()
            {
                Id = 1,
                Content = "{TimestampedGuid}",
                Type = 1,
                ApplicationType = ItemApplicationType.Watermark
            },
            new ItemBaseTemplateModel()
            {
                Id = 2,
                Content = "附加内容",
                Type = 0,
                ApplicationType = ItemApplicationType.Watermark
            },
        };

        private string _loadImagePath = string.Empty;

        private ObservableCollection<ItemBaseTemplateModel> _subDirectoryItems = new()
        {
            new ItemBaseTemplateModel()
            {
                Id = 0,
                Type = 1,
                Content = "{ImageType}",
                ApplicationType = ItemApplicationType.SubDirectory
            },
            new ItemBaseTemplateModel()
            {
                Id = 1,
                Type = 1,
                Content = "{Year}",
                ApplicationType = ItemApplicationType.SubDirectory
            },
            new ItemBaseTemplateModel()
            {
                Id = 2,
                Type = 1,
                Content = "{Month}",
                ApplicationType = ItemApplicationType.SubDirectory
            },
            new ItemBaseTemplateModel()
            {
                Id = 3,
                Type = 1,
                Content = "{Day}",
                ApplicationType = ItemApplicationType.SubDirectory
            },
            new ItemBaseTemplateModel()
            {
                Id = 4,
                Type = 1,
                Content = "{Hour}",
                ApplicationType = ItemApplicationType.SubDirectory
            },
            new ItemBaseTemplateModel()
            {
                Id = 5,
                Type = 1,
                Content = "{CameraSerialNumber}",
                ApplicationType = ItemApplicationType.SubDirectory
            },
        };

        private ObservableCollection<ItemBaseTemplateModel> _imageNamingItems = new()
        {
            new ItemBaseTemplateModel()
            {
                Id = 0,
                Content = "{BarCode}",
                Type = 1,
                ApplicationType = ItemApplicationType.ImageNaming
            },
            new ItemBaseTemplateModel()
            {
                Id = 1,
                Content = "{TimestampedGuid}",
                Type = 1,
                ApplicationType = ItemApplicationType.ImageNaming
            },
        };

        private bool _isSaveBarcodeImage = true;
        private bool _isSavePanoramaImage;
        private bool _isSaveVolumeImage;
        private string _imageRootDirectory = string.Empty;
        private bool _isFtpUploadEnabled;
        private string _ipAddress = string.Empty;
        private int _port;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _isSaveOriginalImage;
        private int _timeout;
        private SnackbarMessageQueue _saveImageSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isSavingInProgress;
        private bool _isLoaded;

        public SaveImageSettingsPageViewModel(IConfigRepository configRepository) {
            _configRepository = configRepository;
            _imageSource = _originalImage;
        }

        public ObservableCollection<ItemBaseTemplateModel> WatermarkItems {
            get => _watermarkItems;
            set => SetProperty(ref _watermarkItems, value);
        }

        public ObservableCollection<ItemBaseTemplateModel> SubDirectoryItems {
            get => _subDirectoryItems;
            set => SetProperty(ref _subDirectoryItems, value);
        }

        public ObservableCollection<ItemBaseTemplateModel> ImageNamingItems {
            get => _imageNamingItems;
            set => SetProperty(ref _imageNamingItems, value);
        }

        /// <summary>
        /// 提示内容
        /// </summary>
        public SnackbarMessageQueue SaveImageSettingsMessageQueue {
            get => _saveImageSettingsMessageQueue;
            set => SetProperty(ref _saveImageSettingsMessageQueue, value);
        }

        public ICommand SliderValueChangedCommand {
            get => new DelegateCommand(SetWatermarkToImage);
        }

        public ICommand ColorPickerValueChangedCommand {
            get => new DelegateCommand(SetWatermarkToImage);
        }

        public ICommand CheckBoxValueChangedCommand {
            get => new DelegateCommand(SetWatermarkToImage);
        }

        public ICommand WatermarkPositionCommand {
            get => new DelegateCommand(SetWatermarkToImage);
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        /// <summary>
        /// 存图根目录
        /// </summary>
        public string ImageRootDirectory {
            get => _imageRootDirectory;
            set => SetProperty(ref _imageRootDirectory, value);
        }

        /// <summary>
        /// 是否保存条码图
        /// </summary>
        public bool IsSaveBarcodeImage {
            get => _isSaveBarcodeImage;
            set => SetProperty(ref _isSaveBarcodeImage, value);
        }

        /// <summary>
        /// 是否保存全景图
        /// </summary>
        public bool IsSavePanoramaImage {
            get => _isSavePanoramaImage;
            set => SetProperty(ref _isSavePanoramaImage, value);
        }

        /// <summary>
        /// 是否保存体积图
        /// </summary>
        public bool IsSaveVolumeImage {
            get => _isSaveVolumeImage;
            set => SetProperty(ref _isSaveVolumeImage, value);
        }

        /// <summary>
        /// 是否保存原图
        /// </summary>
        public bool IsSaveOriginalImage {
            get => _isSaveOriginalImage;
            set => SetProperty(ref _isSaveOriginalImage, value);
        }

        /// <summary>
        /// 图片途径
        /// </summary>
        public string LoadImagePath {
            get => _loadImagePath;
            set => SetProperty(ref _loadImagePath, value);
        }

        /// <summary>
        /// 是否使用水印
        /// </summary>
        public bool IsUseWatermark {
            get => _isUseWatermark;
            set => SetProperty(ref _isUseWatermark, value);
        }

        /// <summary>
        /// 水印内容
        /// </summary>
        public string WatermarkText {
            get => _watermarkText;
            set => SetProperty(ref _watermarkText, value);
        }

        /// <summary>
        /// 是否使用Ftp上传
        /// </summary>
        public bool IsFtpUploadEnabled {
            get => _isFtpUploadEnabled;
            set => SetProperty(ref _isFtpUploadEnabled, value);
        }

        /// <summary>
        /// FtpIp地址
        /// </summary>
        public string IpAddress {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        /// <summary>
        /// Ftp端口号
        /// </summary>
        public int Port {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int Timeout {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }

        /// <summary>
        /// 原图
        /// </summary>
        public ImageSource? OriginalImage {
            get => _originalImage;
            set => SetProperty(ref _originalImage, value);
        }

        /// <summary>
        /// 水印颜色
        /// </summary>
        public System.Windows.Media.Color WatermarkColor {
            get => _watermarkColor;
            set => SetProperty(ref _watermarkColor, value);
        }

        /// <summary>
        /// 水印字体大小
        /// </summary>
        public int WatermarkFontSize {
            get => _watermarkFontSize;
            set => SetProperty(ref _watermarkFontSize, value);
        }

        /// <summary>
        /// 水印位置
        /// </summary>
        public WatermarkPosition WatermarkPosition {
            get => _watermarkPosition;
            set => SetProperty(ref _watermarkPosition, value);
        }

        /// <summary>
        /// 预览图片
        /// </summary>
        public ImageSource? ImageSource {
            get => _imageSource;
            set => SetProperty(ref _imageSource, value);
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
                    //加载配置 SaveImageSettings
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("SaveImageSettings"));
                    if (configInfoModel is not null) {
                        try {
                            WatermarkItems.Clear();
                            SubDirectoryItems.Clear();
                            ImageNamingItems.Clear();
                            var imageSettingsDto = JsonConvert.DeserializeObject<ImageSettingsDto>(configInfoModel.Value);
                            if (imageSettingsDto is not null) {
                                ImageRootDirectory = imageSettingsDto.ImageRootDirectory;
                                IsSaveBarcodeImage = imageSettingsDto.IsSaveBarcodeImage;
                                IsSavePanoramaImage = imageSettingsDto.IsSavePanoramaImage;
                                IsSaveVolumeImage = imageSettingsDto.IsSaveVolumeImage;
                                IsSaveOriginalImage = imageSettingsDto.IsSaveOriginalImage;
                                IsUseWatermark = imageSettingsDto.IsUseWatermark;
                                WatermarkColor = Color.FromRgb(imageSettingsDto.WatermarkInfo.WatermarkColor.R,
                                    imageSettingsDto.WatermarkInfo.WatermarkColor.G,
                                    imageSettingsDto.WatermarkInfo.WatermarkColor.B);
                                WatermarkFontSize = imageSettingsDto.WatermarkInfo.WatermarkFontSize;
                                WatermarkPosition = imageSettingsDto.WatermarkInfo.WatermarkPosition;
                                var templateModels = imageSettingsDto.WatermarkInfo.ItemTemplate.Select((s, i) => new ItemBaseTemplateModel {
                                    ApplicationType = s.ApplicationType,
                                    Content = s.Content,
                                    Type = s.Type,
                                    Id = i + 1,
                                })?.ToList();
                                WatermarkItems.AddRange(templateModels);
                                var models = imageSettingsDto.SubDirectoryTemplate?.Select((s, i) => new ItemBaseTemplateModel() {
                                    ApplicationType = s.ApplicationType,
                                    Content = s.Content,
                                    Type = s.Type,
                                    Id = i + 1
                                }).ToList();
                                SubDirectoryItems.AddRange(models);
                                var list = imageSettingsDto.ImageNamingTemplate.Select((s, i) => new ItemBaseTemplateModel {
                                    ApplicationType = s.ApplicationType,
                                    Content = s.Content,
                                    Type = s.Type,
                                    Id = i + 1,
                                }).ToList();
                                ImageNamingItems.AddRange(list);
                                IsFtpUploadEnabled = imageSettingsDto.IsFtpUploadEnabled;
                                IpAddress = imageSettingsDto.FtpInfo.IpAddress;
                                Port = imageSettingsDto.FtpInfo.Port;
                                Password = imageSettingsDto.FtpInfo.Password;
                                Timeout = imageSettingsDto.FtpInfo.Timeout;
                                Username = imageSettingsDto.FtpInfo.Username;
                            }
                        }
                        catch (Exception e) {
                            SaveImageSettingsMessageQueue.Enqueue($"加载设置失败:{e.Message}");
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 浏览目录
        /// </summary>
        public ICommand OpenFolderCommand {
            get => new DelegateCommand<object>(OpenFolderDelegate);
        }

        private void OpenFolderDelegate(object obj) {
            var folderBrowserDialog = new FolderBrowserDialog() {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                ImageRootDirectory = folderBrowserDialog.SelectedPath;
            }
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
                    // var loadingDialog = new LoadingDialog();
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "SaveImageSettings",
                        Value = JsonConvert.SerializeObject(new ImageSettingsDto {
                            ImageRootDirectory = ImageRootDirectory,
                            IsSaveBarcodeImage = IsSaveBarcodeImage,
                            IsSavePanoramaImage = IsSavePanoramaImage,
                            IsSaveVolumeImage = IsSaveVolumeImage,
                            IsSaveOriginalImage = IsSaveOriginalImage,
                            IsUseWatermark = IsUseWatermark,
                            WatermarkInfo = new WatermarkInfo {
                                WatermarkColor = System.Drawing.Color.FromArgb(WatermarkColor.A,
                                    WatermarkColor.R, WatermarkColor.G, WatermarkColor.B),
                                WatermarkFontSize = WatermarkFontSize,
                                WatermarkPosition = WatermarkPosition,
                                ItemTemplate = WatermarkItems.Select(s => new ItemTemplateInfo {
                                    ApplicationType = s.ApplicationType,
                                    Content = s.Content,
                                    Type = s.Type,
                                }).ToList()
                            },
                            SubDirectoryTemplate = SubDirectoryItems.Select(s => new ItemTemplateInfo() {
                                ApplicationType = s.ApplicationType,
                                Content = s.Content,
                                Type = s.Type,
                            }).ToList(),
                            ImageNamingTemplate = ImageNamingItems.Select(s => new ItemTemplateInfo() {
                                ApplicationType = s.ApplicationType,
                                Content = s.Content,
                                Type = s.Type,
                            }).ToList(),
                            IsFtpUploadEnabled = IsFtpUploadEnabled,
                            FtpInfo = new FtpInfo() {
                                IpAddress = IpAddress,
                                Password = Password,
                                Port = Port,
                                Timeout = Timeout,
                                Username = Username
                            }
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "SaveImageSettings"
                        });
                    }

                    IsSavingInProgress = false;
                    SaveImageSettingsMessageQueue.Enqueue($"保存{(insertOrUpdate ? "成功" : "失败")}");
                });
            }

            //显示遮罩
            //保存设置到数据库
            //通知设置更改事件
            //隐藏遮罩
        }

        /// <summary>
        /// 移除标记
        /// </summary>
        public ICommand RemoveTemplateItemCommand {
            get => new DelegateCommand<ItemBaseTemplateModel>(RemoveTemplateItemDelegate);
        }

        private async void RemoveTemplateItemDelegate(ItemBaseTemplateModel model) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (model.ApplicationType == ItemApplicationType.Watermark) {
                    WatermarkItems.Remove(model);
                    foreach (var item in WatermarkItems) {
                        if (item.Type == 0 && string.IsNullOrEmpty(item.Content) &&
                            WatermarkItems.LastOrDefault() != item) {
                            WatermarkItems.Remove(item);
                        }
                    }

                    WatermarkText = string.Join("", WatermarkItems.Select(s => s.Content));
                    SetWatermarkToImage();
                }
                else if (model.ApplicationType == ItemApplicationType.SubDirectory) {
                    SubDirectoryItems.Remove(model);
                }
                if (model.ApplicationType == ItemApplicationType.ImageNaming) {
                    ImageNamingItems.Remove(model);
                }
            });
        }

        /// <summary>
        /// 添加水印
        /// </summary>
        public ICommand AddWatermarkItemCommand {
            get => new DelegateCommand<string>(AddWatermarkItemDelegate);
        }

        private async void AddWatermarkItemDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                obj = obj.Replace("'", string.Empty);
                var count = WatermarkItems.Count;
                WatermarkItems.Insert(count - 1, new ItemBaseTemplateModel() {
                    Content = obj,
                    Id = count,
                    Type = 1,
                    ApplicationType = ItemApplicationType.Watermark
                });
                var model = WatermarkItems?.LastOrDefault();
                if (model?.Type != 0) {
                    WatermarkItems?.Add(new ItemBaseTemplateModel() {
                        Content = string.Empty,
                        Id = WatermarkItems.Count,
                        ApplicationType = ItemApplicationType.Watermark
                    });
                }
                WatermarkText = string.Join("", WatermarkItems?.Select(s => s.Content) ?? Array.Empty<string>());
                SetWatermarkToImage();
            });
        }

        /// <summary>
        /// 添加子路径
        /// </summary>
        public ICommand AddSubDirectoryItemCommand {
            get => new DelegateCommand<string>(AddSubDirectoryItemDelegate);
        }

        private async void AddSubDirectoryItemDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                obj = obj.Replace("'", string.Empty);
                SubDirectoryItems.Add(new ItemBaseTemplateModel() {
                    Id = SubDirectoryItems.Count,
                    Content = obj,
                    Type = 1,
                    ApplicationType = ItemApplicationType.SubDirectory
                });
            });
        }

        /// <summary>
        /// 添加图片命名元素
        /// </summary>
        public ICommand AddImageNamingItemCommand {
            get => new DelegateCommand<string>(AddImageNamingItemDelegate);
        }

        private async void AddImageNamingItemDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                obj = obj.Replace("'", string.Empty);
                ImageNamingItems.Add(new ItemBaseTemplateModel() {
                    Id = ImageNamingItems.Count,
                    Content = obj,
                    Type = 1,
                    ApplicationType = ItemApplicationType.ImageNaming
                });
            });
        }

        /// <summary>
        /// 加载图片
        /// </summary>
        public ICommand LoadImageCommand {
            get => new DelegateCommand<object>(LoadImageDelegate);
        }

        private async void LoadImageDelegate(object obj) {
            var openFileDialog = new OpenFileDialog() {
                Title = "请选择需要打开的图片",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Filter =
                    "位图文件 (*.bmp; *.gif; *.jpg; *.jpeg; *.png; *.tif; *.tiff)|*.bmp; *.gif; *.jpg; *.jpeg; *.png; *.tif; *.tiff",
                DefaultExt = ".jpg",
                RestoreDirectory = true,
            };
            if (openFileDialog.ShowDialog() == true) {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    OriginalImage = new BitmapImage(new Uri(openFileDialog.FileName));
                    ImageSource = new BitmapImage(new Uri(openFileDialog.FileName));
                    LoadImagePath = openFileDialog.FileName;
                });
            }
        }

        /// <summary>
        /// 设置水印
        /// </summary>
        private void SetWatermarkToImage() {
            Task.Run(async () => {
                //信号锁
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    //组合水印
                    var watermarkTestText = string.Join("\n", WatermarkItems.Select(TestWatermarkConvertGroup));

                    if (OriginalImage is not null && IsUseWatermark && !string.IsNullOrEmpty(watermarkTestText)) {
                        var image = OriginalImage.ConvertImageSourceToImage();
                        if (image is not null) {
                            using var graphics = Graphics.FromImage(image);
                            using var watermarkFont = new Font("Microsoft YaHei", WatermarkFontSize, FontStyle.Bold);
                            using var watermarkBrush = new SolidBrush(System.Drawing.Color.FromArgb(WatermarkColor.A,
                                WatermarkColor.R, WatermarkColor.G, WatermarkColor.B));

                            float x = 0, y = 0;
                            switch (WatermarkPosition) {
                                case WatermarkPosition.TopLeft:
                                    x = 10;
                                    y = 10;
                                    break;

                                case WatermarkPosition.TopRight:
                                    x = image.Width - graphics.MeasureString(watermarkTestText, watermarkFont).Width - 10;
                                    y = 10;
                                    break;

                                case WatermarkPosition.BottomLeft:
                                    x = 10;
                                    y = image.Height - graphics.MeasureString(watermarkTestText, watermarkFont).Height - 10;
                                    break;

                                case WatermarkPosition.BottomRight:
                                    x = image.Width - graphics.MeasureString(watermarkTestText, watermarkFont).Width - 10;
                                    y = image.Height - graphics.MeasureString(watermarkTestText, watermarkFont).Height - 10;
                                    break;

                                default:
                                    x = 10;
                                    y = 10;
                                    break;
                            }
                            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            graphics.DrawString(watermarkTestText, watermarkFont, watermarkBrush, x, y);
                            ImageSource = image.ConvertBitmapToBitmapSource();
                        }
                    }
                });
            });
        }

        private string TestWatermarkConvertGroup(ItemBaseTemplateModel model) {
            if (model.Type == 0 && !string.IsNullOrWhiteSpace(model.Content)) {
                return $"附加:{model.Content}";
            }
            if (model.Type != 1) return string.Empty;
            return model.Content switch {
                "{BarCode}" => $"条码:SF123456789",
                "{Weight}" => $"重量:10.001",
                "{Volume}" => $"体积:100.999",
                "{Length}" => $"长:100.999",
                "{Width}" => $"宽:100.999",
                "{Height}" => $"高:100.999",
                "{ScanTime}" => $"扫码时间:{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                "{TimestampedGuid}" => $"扫码时间戳:{DateTimeOffset.Now.ToUnixTimeMilliseconds()}",
                "{CameraSerialNumber}" => $"相机序列号:ABCDEFG123",
                _ => string.Empty
            };
        }
    }
}