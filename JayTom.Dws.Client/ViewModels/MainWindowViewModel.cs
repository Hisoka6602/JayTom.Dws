using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Regions;
using System.Windows;
using Prism.Commands;
using System.Threading;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.ViewModels {

    public class MainWindowViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        private double _uniformCornerRadius = 5;
        private string _maxBtnIcon = "\xe600";
        private string _maxBtnToolTip = "Maximize";
        private SnackbarMessageQueue _mainMessageQueue = new(TimeSpan.FromSeconds(2));
        private string _requestStatus = string.Empty;
        private string _displayBarcode = string.Empty;

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