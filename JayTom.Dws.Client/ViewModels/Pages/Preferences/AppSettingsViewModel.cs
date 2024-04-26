using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Regions;
using System.Windows;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class AppSettingsViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        private ObservableCollection<MenuItemInfoModel> _appSettingsMenuItems;
        private static bool _isLoaded;

        public AppSettingsViewModel(IRegionManager regionManager) {
            _regionManager = regionManager;
            _appSettingsMenuItems = new ObservableCollection<MenuItemInfoModel>()
            {
                new()
                {
                    Title = "其他设置",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe84e",
                        IconSize = 25
                    },
                    IsSelected = true,
                    Description = "其他设置",
                    PageClassName = "OtherSettingsPage",
                    ClickCommand = ClickCommand
                },
                /*new()
                {
                    Title = "列表设置",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe849",
                        IconSize = 25
                    },
                    Description = "列表设置",
                    PageClassName = "GridSettingsPage",
                    ClickCommand = ClickCommand
                },*/
                new()
                {
                    Title = "授权信息",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe90f",
                        IconSize = 25
                    },
                    Description = "授权信息",
                    PageClassName = "LicensePage",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Title = "设置同步",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe957",
                        IconSize = 25
                    },
                    Description = "设置同步",
                    PageClassName = "SyncSettingsPage",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Title = "密码设置",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe983",
                        IconSize = 25
                    },
                    Description = "密码设置",
                    PageClassName = "PassWordSettingsPage",
                    ClickCommand = ClickCommand
                },
            };
        }

        public ObservableCollection<MenuItemInfoModel> AppSettingsMenuItems {
            get => _appSettingsMenuItems;
            set => SetProperty(ref _appSettingsMenuItems, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<Frame>(LoadedDelegate);
        }

        private async void LoadedDelegate(Frame obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    if (!_regionManager.Regions.ContainsRegionWithName("AppSettingsRegion")) {
                        //创建区域(用于视觉树以外控件)
                        RegionManager.SetRegionName(obj, "AppSettingsRegion");
                        RegionManager.SetRegionManager(obj, _regionManager);
                    }
                    _regionManager.Regions["AppSettingsRegion"].RequestNavigate("OtherSettingsPage");
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
                    foreach (var item in AppSettingsMenuItems) {
                        item.IsSelected = false;
                    }

                    obj.IsSelected = true;

                    _regionManager?.Regions?["AppSettingsRegion"]
                        ?.RequestNavigate(new Uri(obj.PageClassName, UriKind.Relative));
                }
            }, DispatcherPriority.Background);
        }
    }
}