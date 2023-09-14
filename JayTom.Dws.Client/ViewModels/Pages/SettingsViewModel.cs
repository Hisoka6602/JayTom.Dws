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
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.ViewModels.Dialog;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class SettingsViewModel : BindableBase {
        private readonly IRegionManager _regionManager;

        private ObservableCollection<MenuItemInfoModel> _menuItems;

        public SettingsViewModel(IRegionManager regionManager) {
            _regionManager = regionManager;
            _menuItems = new()
            {
                /*new MenuItemInfoModel()
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
                },*/
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe6da",
                        IconSize = 22
                    },
                    Title = Languages.Language.ResourceManager.GetString("DataManagement")??string.Empty,
                    Description = "数据信息管理",
                    ClickCommand = ClickCommand,
                    PageClassName = "DataManagementPage",
                    IsSelected = true
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe63a",
                        IconSize = 25
                    },
                    Title = Languages.Language.ResourceManager.GetString("CameraConfiguration")??string.Empty,
                    Description = "相机管理配置",
                    ClickCommand = ClickCommand,
                    PageClassName = "CameraConfigurationPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe664",
                        IconSize = 25
                    },
                    Title = Languages.Language.ResourceManager.GetString("ApiInterface")??string.Empty,
                    Description = "Api上传接口设置",
                    ClickCommand = ClickCommand,
                    PageClassName = "APISettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe6b1",
                        IconSize = 23
                    },
                    Title = Languages.Language.ResourceManager.GetString("ImageStorageSettings")??string.Empty,
                    Description = "存图相关设置",
                    ClickCommand = ClickCommand,
                    PageClassName = "SaveImageSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe6af",
                        IconSize = 30
                    },
                    Title = Languages.Language.ResourceManager.GetString("FilteringSettings")??string.Empty,
                    Description = "条码过滤相关设置",
                    ClickCommand = ClickCommand,
                    PageClassName = "BarcodeFilterSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe71e",
                        IconSize = 26
                    },
                    Title = Languages.Language.ResourceManager.GetString("WeightSettings")??string.Empty,
                    Description = "磅秤和称重设置相关",
                    ClickCommand = ClickCommand,
                    PageClassName = "WeightSettingPages"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe72f",
                        IconSize = 28
                    },
                    Title = Languages.Language.ResourceManager.GetString("VolumeSettings")??string.Empty,
                    Description = "体积设置相关",
                    ClickCommand = ClickCommand,
                    PageClassName = "VolumeSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe641",
                        IconSize = 30
                    },
                    Title = Languages.Language.ResourceManager.GetString("OutputResults")??string.Empty,
                    Description = "结果输出相关设置",
                    ClickCommand = ClickCommand,
                    PageClassName = "ResultOutputSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe6b5",
                        IconSize = 30
                    },
                    Title = Languages.Language.ResourceManager.GetString("InputContent")??string.Empty,
                    Description = "内容输入相关设置",
                    ClickCommand = ClickCommand,
                    PageClassName = "ContentInputSettingsPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xeaad",
                        IconSize = 25
                    },
                    Title = Languages.Language.ResourceManager.GetString("LogManagement")??string.Empty,
                    Description = "运行日志、设备日志、通讯日志管理",
                    ClickCommand = ClickCommand,
                    PageClassName = "LogManagerPage"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe74a",
                        IconSize = 25
                    },
                    Title = Languages.Language.ResourceManager.GetString("CacheCearing")??string.Empty,
                    Description = "释放缓存/空间的方案",
                    ClickCommand = ClickCommand,
                    PageClassName = "CacheClearSettingsPage"
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
                //加载loading
                var loadingDialog = new LoadingDialog();
                if (loadingDialog.DataContext is LoadingDialogViewModel model) {
                    model.Identifier = "SettingDialog";
                    DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
                    if (!_regionManager.Regions.ContainsRegionWithName("ContentRegion")) {
                        //创建区域(用于视觉树以外控件)
                        RegionManager.SetRegionName(obj, "ContentRegion");
                        RegionManager.SetRegionManager(obj, _regionManager);
                        _regionManager.Regions["ContentRegion"].RequestNavigate("DataManagementPage");
                    }
                    if (DialogHost.IsDialogOpen(model.Identifier)) {
                        DialogHost.Close(model.Identifier);
                    }
                }
            });
        }

        private void MenuClickDelegate(MenuItemInfoModel obj) {
            //加载loading
            Task.Run(async () => {
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    var loadingDialog = new LoadingDialog();
                    if (loadingDialog.DataContext is LoadingDialogViewModel model) {
                        model.Identifier = "SettingDialog";
                        DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
                        await Task.Delay(400);
                        //跳转
                        {
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
                        }
                        if (DialogHost.IsDialogOpen(model.Identifier)) {
                            DialogHost.Close(model.Identifier);
                        }
                    }
                });
            });
        }
    }
}