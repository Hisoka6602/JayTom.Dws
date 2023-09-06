using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Regions;
using System.Windows;
using Prism.Commands;
using System.Threading;
using System.Globalization;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Windows.Controls;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.PluginInterface.Utils;

namespace JayTom.Dws.Client.ViewModels {

    public class MainWindowViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        private readonly IDialogService _dialogService;
        private double _uniformCornerRadius = 5;
        private string _maxBtnIcon = "\xe600";
        private string _maxBtnToolTip = "Maximize";
        private SnackbarMessageQueue _mainMessageQueue = new(TimeSpan.FromSeconds(2));
        private string _requestStatus = string.Empty;
        private string _displayBarcode = string.Empty;
        private Point _buttonTranslateTransform = new(0, 0);
        private Size _menuButtonSizeize = new(0, 0);
        private bool _isLoaded;
        private ObservableCollection<HomeToolInfoModel> _homeToolItems = new();

        public MainWindowViewModel(IRegionManager regionManager, IDialogService dialogService) {
            _regionManager = regionManager;
            _dialogService = dialogService;
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
                    IsModal = true,
                    OpenCommand = OpenHomeToolCommand
                }
            };
        }

        public ObservableCollection<HomeToolInfoModel> HomeToolItems {
            get => _homeToolItems;
            set => SetProperty(ref _homeToolItems, value);
        }

        public double UniformCornerRadius {
            get => _uniformCornerRadius;
            set => SetProperty(ref _uniformCornerRadius, value);
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

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        public ICommand MinWinCommand {
            get => new DelegateCommand<object>(MinWinDelegate);
        }

        public ICommand MaxWinCommand {
            get => new DelegateCommand<object>(MaxWinDelegate);
        }

        public ICommand CloseWinCommand {
            get => new DelegateCommand<object>(CloseWinDelegate);
        }

        public ICommand HomeToolSelectionChangedCommand {
            get => new DelegateCommand<ComboBox>(HomeToolSelectionChangedDelegate);
        }

        private void HomeToolSelectionChangedDelegate(ComboBox obj) {
            obj.SelectedIndex = 0;
        }

        public ICommand OpenHomeToolCommand {
            get => new DelegateCommand<HomeToolInfoModel>(OpenHomeToolDelegate);
        }

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

        private void CloseWinDelegate(object obj) {
            System.Windows.Application.Current.Shutdown();//关闭
        }

        private void MaxWinDelegate(object obj) {
            if (obj is Window window) {
                if (window.WindowState == WindowState.Maximized) {
                    window.WindowState = WindowState.Normal;
                    return;
                }
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
            }
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                await Task.Delay(TimeSpan.FromSeconds(5));
                //加载配置需要有一个事件通知各个模块
                //加载体积配置
                //加载重量配置
                IsLoaded = true;
            });
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
                window.WindowState = WindowState.Minimized;
            }
        }
    }
}