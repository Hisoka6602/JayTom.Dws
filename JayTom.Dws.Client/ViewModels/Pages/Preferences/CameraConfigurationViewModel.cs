using System;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using System.Windows.Threading;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class CameraConfigurationViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        private static bool _isLoaded;
        private ObservableCollection<MenuItemInfoModel> _cameraMenuItems = new();

        public CameraConfigurationViewModel(IRegionManager regionManager) {
            _regionManager = regionManager;
            _cameraMenuItems = new()
        {
            new MenuItemInfoModel()
            {
                Title = Languages.Language.ResourceManager.GetString("CameraSearch")??string.Empty,
                IconFont = new IconInfoModel()
                {
                    IconFont = "pack://application:,,,/Fonts/#iconfont",
                    IconCode = "\xe684",
                    IconSize = 25
                },
                Description = Languages.Language.ResourceManager.GetString("发现所有相机")??string.Empty,
                IsSelected = true,
                PageClassName = "CameraFinderPage",
                ClickCommand = ClickCommand,
            },
            new MenuItemInfoModel()
            {
                Title = Languages.Language.ResourceManager.GetString("PanoramaCamera")??string.Empty,
                IconFont = new IconInfoModel()
                {
                    IconFont = "pack://application:,,,/Fonts/#iconfont",
                    IconCode = "\xe6bc",
                    IconSize = 25
                },
                Description = Languages.Language.ResourceManager.GetString("全景相机配置")??string.Empty,
                PageClassName = "PanoramaCameraConfigPage",
                ClickCommand = ClickCommand,
            },
            new MenuItemInfoModel()
            {
                Title = Languages.Language.ResourceManager.GetString("ScannerCamera")??string.Empty,
                IconFont = new IconInfoModel()
                {
                    IconFont = "pack://application:,,,/Fonts/#iconfont",
                    IconCode = "\xe662",
                    IconSize = 22
                },
                Description = Languages.Language.ResourceManager.GetString("扫码相机配置") ?? string.Empty,
                PageClassName = "BarcodeScannerCameraConfigPage",
                ClickCommand = ClickCommand,
            },
            new MenuItemInfoModel()
            {
                Title = Languages.Language.ResourceManager.GetString("CompactCamera")??string.Empty,
                IconFont = new IconInfoModel()
                {
                    IconFont = "pack://application:,,,/Fonts/#iconfont",
                    IconCode = "\xe665",
                    IconSize = 25
                },
                Description = Languages.Language.ResourceManager.GetString("体积相机配置") ?? string.Empty,
                PageClassName = "VolumeCameraConfigPage",
                ClickCommand = ClickCommand,
            },
        };
        }

        public ObservableCollection<MenuItemInfoModel> CameraMenuItems {
            get => _cameraMenuItems;
            set => SetProperty(ref _cameraMenuItems, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<Frame>(LoadedDelegate);
        }

        private async void LoadedDelegate(Frame obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    if (!_regionManager.Regions.ContainsRegionWithName("CameraConfigRegion")) {
                        //创建区域(用于视觉树以外控件)
                        RegionManager.SetRegionName(obj, "CameraConfigRegion");
                        RegionManager.SetRegionManager(obj, _regionManager);
                    }
                    _regionManager.Regions["CameraConfigRegion"].RequestNavigate("CameraFinderPage");
                });
            }
        }

        /// <summary>
        /// 点击事件
        /// </summary>
        public ICommand ClickCommand {
            get => new DelegateCommand<MenuItemInfoModel>(MenuClickDelegate);
        }

        private async void MenuClickDelegate(MenuItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                if (!obj.PageClassName.Equals(string.Empty)) {
                    foreach (var item in CameraMenuItems) {
                        item.IsSelected = false;
                    }

                    obj.IsSelected = true;

                    _regionManager?.Regions?["CameraConfigRegion"]
                        ?.RequestNavigate(new Uri(obj.PageClassName, UriKind.Relative));
                }
            }, DispatcherPriority.Background);
        }
    }
}