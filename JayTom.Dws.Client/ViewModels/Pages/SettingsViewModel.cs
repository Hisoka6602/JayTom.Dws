using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Regions;
using System.Drawing;
using Prism.Commands;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.ViewModels.Pages {
    public class SettingsViewModel : BindableBase {
        private readonly IRegionManager _regionManager;

        private ObservableCollection<MenuItemInfoModel> _menuItems;

        public SettingsViewModel(IRegionManager regionManager) {
            _regionManager = regionManager;
            _menuItems = new()
            {
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe69d",
                        IconSize = 25,
                    },
                    Title = "插件信息",
                    Description = "灵活下载/组合插件插件",
                    ClickCommand = ClickCommand,
                    PageClassName = "PluginMarketplacePage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe6da",
                        IconSize = 22
                    },
                    Title = "数据管理",
                    Description = "数据信息管理",
                    ClickCommand = ClickCommand,
                    PageClassName = "DataManagementPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe63a",
                        IconSize = 25
                    },
                    Title = "相机配置",
                    Description = "相机管理配置",
                    ClickCommand = ClickCommand,
                    PageClassName = "CameraConfigurationPage"
                },
                //&#xe63a;
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe664",
                        IconSize = 25
                    },
                    Title = "Api接口",
                    Description = "Api上传接口设置",
                    ClickCommand = ClickCommand
                },
            };
        }

        //MenuItems
        public ObservableCollection<MenuItemInfoModel> MenuItems {
            get => _menuItems;
            set => SetProperty(ref _menuItems, value);
        }

        /// <summary>
        /// 点击事件
        /// </summary>
        public ICommand ClickCommand {
            get => new DelegateCommand<MenuItemInfoModel>(MenuClickDelegate);
        }

        /// <summary>
        /// 窗口加载完成
        /// </summary>
        public ICommand LoadedCommand {
            get => new DelegateCommand<Frame>(LoadedDelegate);
        }

        private async void LoadedDelegate(Frame obj) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                if (!_regionManager.Regions.ContainsRegionWithName("ContentRegion")) {
                    //创建区域(用于视觉树以外控件)
                    RegionManager.SetRegionName(obj, "ContentRegion");
                    RegionManager.SetRegionManager(obj, _regionManager);
                    _regionManager.Regions["ContentRegion"].RequestNavigate("DataManagementPage");
                }
            });
        }

        private async void MenuClickDelegate(MenuItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                if (!obj.PageClassName.Equals(string.Empty)) {
                    foreach (var item in MenuItems) {
                        item.IsSelected = false;
                        item.RadiusRight = new CornerRadius(0, 0, 0, 0);
                    }
                    obj.IsSelected = true;
                    MenuItemInfoModel? previousItem = null, nextItem = null;
                    var of = MenuItems.IndexOf(obj);
                    if (of - 1 >= 0) {
                        //有前一个
                        previousItem = MenuItems[of - 1];
                    }
                    if (of < MenuItems.Count - 1) {
                        nextItem = MenuItems[of + 1];
                    }

                    if (previousItem is not null) {
                        previousItem.RadiusRight = new CornerRadius(0, 0, 10, 0);
                    }

                    if (nextItem is not null) {
                        nextItem.RadiusRight = new CornerRadius(0, 10, 0, 0);
                    }

                    _regionManager?.Regions?["ContentRegion"]?.RequestNavigate(new Uri(obj.PageClassName, UriKind.Relative));
                }
            }, DispatcherPriority.Background);
        }
    }
}