using System;
using Prism.Mvvm;
using Prism.Regions;
using System.Windows;
using Prism.Commands;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace JayTom.Dws.TemporaryClient.ViewModels {

    public class MainWindowViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        private double _uniformCornerRadius = 10;
        private string _maxBtnIcon = "\xe600";
        private string _maxBtnToolTip = "最大化";
        private SnackbarMessageQueue _mainMessageQueue = new(TimeSpan.FromSeconds(2));

        public MainWindowViewModel(IRegionManager regionManager) {
            _regionManager = regionManager;
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

        public ICommand SizeChangedCommand {
            get => new DelegateCommand<object>(SizeChangeDelegate);
        }

        public ICommand PageSwitchingCommand {
            get => new DelegateCommand<object>(PageSwitchingDelegate);
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

        private void SizeChangeDelegate(object obj) {
            if (obj is Window window) {
                window.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                window.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                if (window.WindowState == WindowState.Maximized ||
                    (window.Height >= SystemParameters.MaximizedPrimaryScreenHeight &&
                     window.Width >= SystemParameters.MaximizedPrimaryScreenWidth)) {
                    //直角
                    UniformCornerRadius = 0;
                }
                else {
                    UniformCornerRadius = 10;
                    //圆角
                }
                if (window.WindowState == WindowState.Maximized) {
                    MaxBtnIcon = "\xe72c";
                    MaxBtnToolTip = "还原";
                }
                else {
                    MaxBtnIcon = "\xe600";
                    MaxBtnToolTip = "最大化";
                }
            }
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
            /*await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                _regionManager.Regions["ContentRegion"].RequestNavigate("HomeView");
            });*/
        }

        private void MinWinDelegate(object obj) {
            if (obj is Window window) {
                window.WindowState = WindowState.Minimized;
            }
        }
    }
}