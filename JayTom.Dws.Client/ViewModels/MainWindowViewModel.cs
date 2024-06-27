using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using Prism.Regions;
using Prism.Commands;
using System.Windows;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using Prism.Services.Dialogs;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalConf;
using Size = System.Windows.Size;
using System.Windows.Media.Imaging;
using JayTom.Dws.Domain.Dto.AppDto;
using Point = System.Windows.Point;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Service.SyncSettings;
using JayTom.Dws.Client.Models.AppSettingModel;
using JayTom.Dws.Infrastructure.Repository.LocalConf;

namespace JayTom.Dws.Client.ViewModels {

    public class MainWindowViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        private readonly IDialogService _dialogService;
        private readonly IConfigRepository _configRepository;
        private readonly ISyncSettingsService _syncSettingsService;
        private double _uniformCornerRadius = 5;
        private string _maxBtnIcon = "\xe600";
        private string _maxBtnToolTip = "Maximize";
        private SnackbarMessageQueue _mainMessageQueue = new(TimeSpan.FromSeconds(2));
        private string _requestStatus = string.Empty;
        private string _displayBarcode = string.Empty;
        private Point _buttonTranslateTransform = new(0, 0);
        private Size _menuButtonSizeize = new(0, 0);
        private bool _isLoaded;
        private SyncSettingsDto _syncSettingsDto = new();
        private ObservableCollection<HomeToolInfoModel> _homeToolItems = new();

        private LanguageInfoModel? _selectedLanguage = new() {
            DisplayName = "中文",
            NationalFlag = new BitmapImage() {
                UriSource = new Uri("pack://application:,,,/Image/NationalFlag/Chinese national flag.png"),
                DecodePixelHeight = 25,
                DecodePixelWidth = 25,
            }
        };

        private string _programTitle = "DWS";
        private ImageSource? _logoSource = null;

        public MainWindowViewModel(IRegionManager regionManager,
            IDialogService dialogService,
            IConfigRepository configRepository,
            ISyncSettingsService syncSettingsService) {
            _regionManager = regionManager;
            _dialogService = dialogService;
            _configRepository = configRepository;
            _syncSettingsService = syncSettingsService;
            HomeToolItems = new ObservableCollection<HomeToolInfoModel>()
            {
                new()
                {
                    Name = "ToolPlugin",
                    Brief = "ToolPlugin",
                    IsRunnable = false
                },
                new()
                {
                    Name = "InputBarcodeControl",
                    Brief = "InputBarcodeControl",
                    ControlClassName = "SunnenInputBarcodeControl",
                    IsRunnable = true,
                    IsModal = false,
                    OpenCommand = OpenHomeToolCommand
                }
            };
            EventAggregator.Instance.Subscribe<RemoteAction>(async item => {
                if (item is RemoteAction remoteAction) {
                    switch (remoteAction.Command) {
                        case RemoteCommand.Exit:
                            CloseWinDelegate(null);
                            break;
                    }
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is SettingsChangedEvent { SettingsName: "SyncSettingsSettings" } syncSettingsSettings) {
                    _syncSettingsDto = await _configRepository.FirstOrDefaultEntity<SyncSettingsDto>(syncSettingsSettings.SettingsName) ?? new SyncSettingsDto();
                    if (_syncSettingsDto.IsUseSyncSettings && !string.IsNullOrEmpty(_syncSettingsDto.Url)) {
                        //连接
                        if (!_syncSettingsService.IsConnected) {
                            var (key, value) = await _syncSettingsService.Connect(_syncSettingsDto.Url);
                            MainMessageQueue.Enqueue($"同步配置连接{(key ? "成功" : "失败")}");
                        }
                    }
                    else {
                        _syncSettingsService.Disconnect();
                    }
                }
            });
        }

        public ObservableCollection<HomeToolInfoModel> HomeToolItems {
            get => _homeToolItems;
            set => SetProperty(ref _homeToolItems, value);
        }

        public LanguageInfoModel? SelectedLanguage {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }

        public double UniformCornerRadius {
            get => _uniformCornerRadius;
            set => SetProperty(ref _uniformCornerRadius, value);
        }

        public string ProgramTitle {
            get => _programTitle;
            set => SetProperty(ref _programTitle, value);
        }

        public ImageSource? LogoSource {
            get => _logoSource;
            set => SetProperty(ref _logoSource, value);
        }

        /// <summary>
        /// 最大化按钮图标
        /// </summary>
        public string MaxBtnIcon {
            get => _maxBtnIcon;
            set => SetProperty(ref _maxBtnIcon, value);
        }

        /// <summary>
        /// 最大化按钮提示内容
        /// </summary>
        public string MaxBtnToolTip {
            get => _maxBtnToolTip;
            set => SetProperty(ref _maxBtnToolTip, value);
        }

        public Point ButtonTranslateTransform {
            get => _buttonTranslateTransform;
            set => SetProperty(ref _buttonTranslateTransform, value);
        }

        /// <summary>
        /// 是否加载完成
        /// </summary>
        public bool IsLoaded {
            get => _isLoaded;
            set => SetProperty(ref _isLoaded, value);
        }

        /// <summary>
        /// 提示内容
        /// </summary>
        public SnackbarMessageQueue MainMessageQueue {
            get => _mainMessageQueue;
            set => SetProperty(ref _mainMessageQueue, value);
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        public ICommand MinWinCommand => new DelegateCommand<object>(MinWinDelegate);

        public ICommand MaxWinCommand => new DelegateCommand<object>(MaxWinDelegate);

        public ICommand CloseWinCommand => new DelegateCommand<object>(CloseWinDelegate);

        public ICommand HomeToolSelectionChangedCommand => new DelegateCommand<ComboBox>(HomeToolSelectionChangedDelegate);

        private void HomeToolSelectionChangedDelegate(ComboBox obj) {
            obj.SelectedIndex = 0;
        }

        public ICommand OpenHomeToolCommand => new DelegateCommand<HomeToolInfoModel>(OpenHomeToolDelegate);

        private void OpenHomeToolDelegate(HomeToolInfoModel obj) {
            //判断是否模态窗口
            if (!string.IsNullOrEmpty(obj.ControlClassName)) {
                if (obj.IsModal) {
                    _dialogService.ShowDialog(obj.ControlClassName);
                }
                else {
                    _dialogService.Show(obj.ControlClassName);
                }
            }
        }

        /// <summary>
        /// 页面切换
        /// </summary>
        /// <param name="obj"></param>
        private async void PageSwitchingDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                _regionManager.Regions["ContentRegion"].RequestNavigate(obj.ToString());
            });
        }

        private async void CloseWinDelegate(object obj) {
            EventAggregator.Instance.Publish(new WindowsAction {
                Type = WindowsActionType.Close
            });
            await Task.Delay(60);
            System.Windows.Application.Current.Shutdown();//关闭
        }

        private void MaxWinDelegate(object obj) {
            if (obj is Window window) {
                if (window.WindowState == WindowState.Maximized) {
                    EventAggregator.Instance.Publish(new WindowsAction {
                        Type = WindowsActionType.Restore
                    });
                    window.WindowState = WindowState.Normal;
                    return;
                }
                EventAggregator.Instance.Publish(new WindowsAction {
                    Type = WindowsActionType.Maximize
                });
                window.WindowState = WindowState.Maximized;
            }
        }

        private async void LoadedDelegate(object obj) {
            if (obj is Window window) {
                window.SizeChanged += SizeChangeDelegate;

                var visualChild = PluginInterface.Utils.Utils.GetVisualChild<Button>(window, b => b.Name.Equals("MenuButton"));
                if (visualChild is not null) {
                    //设置变化值
                    _menuButtonSizeize = new Size(visualChild.ActualWidth, visualChild.ActualHeight);
                    ButtonTranslateTransform =
                        new Point((window.ActualWidth - _menuButtonSizeize.Width) / 2,
                            (window.ActualHeight - _menuButtonSizeize.Height) / 2);
                    visualChild.Visibility = Visibility.Visible;
                }
                EventAggregator.Instance.Publish(new WindowsAction {
                    Type = WindowsActionType.Activate
                });
            }
            //加载语言选择

            var language = (await _configRepository.
                FirstOrDefault(f => f.ConfigName.Equals("SelectedLanguage")))
                ?.Value;
            //加载程序设置
            var configInfoModel = await _configRepository.FirstOrDefault(f =>
                f.ConfigName.Equals("OtherSettings"));
            if (configInfoModel is not null) {
                try {
                    var otherSettingsDto = JsonConvert.DeserializeObject<OtherSettingsDto>(configInfoModel.Value);
                    if (otherSettingsDto is not null) {
                        //加载图片
                        if (File.Exists(otherSettingsDto.ProgramLogoPath)) {
                            LogoSource = JayTom.Dws.PluginInterface.Utils.Utils.CreateBitmapImage(new Uri(otherSettingsDto.ProgramLogoPath), 148, 148);
                        }
                        ProgramTitle = otherSettingsDto.ProgramTitle;
                        //最大化
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            if (obj is Window windows && otherSettingsDto.IsAutoMaximize) {
                                windows.WindowState = WindowState.Maximized;
                            }
                        });
                    }
                }
                catch (Exception e) {
                    EventAggregator.Instance.Publish(new AppLogInfoModel {
                        CreateTime = DateTime.Now,
                        Message = $"加载程序配置错误:{e.Message}",
                        Type = LogType.Exception
                    });
                }
            }

            await Application.Current.Dispatcher.InvokeAsync(async () => {
                NLog.LogManager.GetCurrentClassLogger().Error($"进入主页加载");
                await Task.Delay(TimeSpan.FromSeconds(5));
                //加载配置需要有一个事件通知各个模块
                //加载体积配置
                //加载重量配置
                var models = (LanguageInfoModel[])Application.Current.Resources["LanguageInfoArray"];
                var languageInfoModel = models.FirstOrDefault(f => f.DisplayName.Equals(language));
                if (languageInfoModel is not null) {
                    SelectedLanguage = languageInfoModel;
                }

                if (System.Windows.Forms.Screen.PrimaryScreen != null &&
                    System.Windows.Forms.Screen.PrimaryScreen?.Bounds is not null) {
                    var screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                    var screenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

                    // 获取当前屏幕DPI
                    var hdc = GetDC(IntPtr.Zero);
                    var dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
                    var dpiY = GetDeviceCaps(hdc, LOGPIXELSY);
                    // 计算DPI调整后的分辨率
                    var adjustedScreenWidth = (int)(screenWidth * 96f / dpiX);
                    var adjustedScreenHeight = (int)(screenHeight * 96f / dpiY);
                    if (adjustedScreenWidth < 1820 ||
                        adjustedScreenHeight < 900) {
                        var resolutionConstraintDialog = new ResolutionConstraintDialog();
                        if (resolutionConstraintDialog.DataContext is ResolutionConstraintViewModel model) {
                            model.Identifier = "MainDialog";
                            model.MinimumWidth = 1820;
                            model.MinimumHeight = 900;
                            //暂时先不提示分辨率
                            /*await DialogHost.Show(resolutionConstraintDialog, model.Identifier);
                            if (!model.ContinueRunning) {
                                System.Windows.Application.Current.Shutdown();//关闭
                            }*/
                            if (obj is Window windows) {
                                windows.WindowState = WindowState.Maximized;
                            }
                        }
                    }
                }

                IsLoaded = true;

                //连接同步配置

                _syncSettingsDto = await _configRepository.FirstOrDefaultEntity<SyncSettingsDto>("SyncSettingsSettings") ??
                                   new SyncSettingsDto();
                if (_syncSettingsDto.IsUseSyncSettings && !string.IsNullOrEmpty(_syncSettingsDto.Url)) {
                    //连接
                    if (!_syncSettingsService.IsConnected) {
                        var (key, value) = await _syncSettingsService.Connect(_syncSettingsDto.Url);
                        MainMessageQueue.Enqueue($"同步配置连接{(key ? "成功" : "失败")}");
                    }
                }
                NLog.LogManager.GetCurrentClassLogger().Error($"完成主页加载");
            });

            //连接同步配置
        }

        private void SizeChangeDelegate(object sender, SizeChangedEventArgs e) {
            if (sender is Window window) {
                window.MaxHeight = SystemParameters.WorkArea.Width;
                window.MaxHeight = SystemParameters.WorkArea.Height;
                /*window.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 5;
                window.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;*/
                if (window.WindowState == WindowState.Maximized ||
                    (window.Height >= SystemParameters.WorkArea.Height &&
                     window.Width >= SystemParameters.WorkArea.Width)) {
                    //直角
                    UniformCornerRadius = 0;
                }
                else {
                    UniformCornerRadius = 5;
                    //圆角
                }
                if (window.WindowState == WindowState.Maximized) {
                    MaxBtnIcon = "\xe72c";
                    MaxBtnToolTip = "Restore";
                }
                else {
                    MaxBtnIcon = "\xe600";
                    MaxBtnToolTip = "Maximize";
                }

                if (!IsLoaded) {
                    var visualChild = PluginInterface.Utils.Utils.GetVisualChild<Button>(window, b => b.Name.Equals("MenuButton"));
                    if (visualChild is not null) {
                        //设置变化值
                        ButtonTranslateTransform =
                            new Point((window.ActualWidth - _menuButtonSizeize.Width) / 2,
                                (window.ActualHeight - _menuButtonSizeize.Height) / 2);
                    }
                }
            }
        }

        private void MinWinDelegate(object obj) {
            if (obj is Window window) {
                EventAggregator.Instance.Publish(new WindowsAction {
                    Type = WindowsActionType.Minimize
                });
                window.WindowState = WindowState.Minimized;
            }
        }

        public ICommand LanguageSwitchCommand => new DelegateCommand<object>(LanguageSwitchDelegate);

        private async void LanguageSwitchDelegate(object obj) {
            CultureInfo? culture = null;
            /*在代码中引用资源文件中的翻译文本。使用资源绑定来访问文本值，或者直接通过代码访问。
            示例：string translatedText = Strings.ResourceManager.GetString("Hello");*/
            if (obj is LanguageInfoModel model && IsLoaded) {
                if (model.DisplayName.Equals("中文")) {
                    culture = new CultureInfo("zh-CN");
                }
                else if (model.DisplayName.Equals("English")) {
                    culture = new CultureInfo("en-US");
                }

                if (culture is not null) {
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;

                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "Language",
                        Value = culture.Name,
                    });
                    if (insertOrUpdate) {
                        await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                            ConfigName = "SelectedLanguage",
                            Value = SelectedLanguage?.DisplayName ?? string.Empty,
                        });
                    }
                    MainMessageQueue.Enqueue(insertOrUpdate ? Languages.Language.ResourceManager.GetString("切换语言成功提示") : Languages.Language.ResourceManager.GetString("切换语言失败提示"));
                }

                EventAggregator.Instance.Publish(new AppLogInfoModel {
                    CreateTime = DateTime.Now,
                    Message = $"切换语言:{model.DisplayName}",
                    Type = LogType.Information
                });
            }
        }

        [DllImport("User32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("User32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);

        [DllImport("Gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;
    }
}