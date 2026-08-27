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

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences
{

    public class PackageSortingSettingsViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private ObservableCollection<MenuItemInfoModel> _packageSortingMenuItems;
        private static bool _isLoaded;

        public PackageSortingSettingsViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            _packageSortingMenuItems = new ObservableCollection<MenuItemInfoModel>()
            {
                new()
                {
                    Title = "下位机通讯",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe7ce",
                        IconSize = 25
                    },
                    Description = "下位机通讯设置",
                    PageClassName = "CommunicationsSettingsPage",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Title = "格口定义",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe7c0",
                        IconSize = 25
                    },
                    Description = "定义包裹流出位置",
                    IsSelected = true,
                    PageClassName = "PackageExitDefinitionPage",
                    ClickCommand = ClickCommand
                },

                new()
                {
                    Title = "指令绑定",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe7b8",
                        IconSize = 25
                    },
                    Description = "对应格口指令绑定",
                    PageClassName = "SortingInstructionBindingPage",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Title = "物流识别",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe7a8",
                        IconSize = 25
                    },
                    Description = "识别单号对应物流公司",
                    PageClassName = "LogisticsCodeRecognitionPage",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Title = "分拣模式",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe7b9",
                        IconSize = 25
                    },
                    Description = "分拣依据选择",
                    PageClassName = "SortingMethodPage",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Title = "锁格设置",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe92e",
                        IconSize = 25
                    },
                    Description = "锁格/解锁设置",
                    PageClassName = "PackageExitLockSettingsPage",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Title = "叠包检测",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe942",
                        IconSize = 25
                    },
                    Description = "叠包检测/供包台多包裹检测",
                    PageClassName = "StackedPackageDetectionSettingsPage",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Title = "供包台模式",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe981",
                        IconSize = 25
                    },
                    Description = "供包台模式相关设置",
                    PageClassName = "SupplyCounterSettingsPage",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Title = "灰度仪",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe9a5",
                        IconSize = 25
                    },
                    Description = "灰度仪相关设置",
                    PageClassName = "GrayscaleDeviceSettingsPage",
                    ClickCommand = ClickCommand
                },
            };
        }

        public ObservableCollection<MenuItemInfoModel> PackageSortingMenuItems
        {
            get => _packageSortingMenuItems;
            set => SetProperty(ref _packageSortingMenuItems, value);
        }

        public ICommand LoadedCommand => new DelegateCommand<Frame>(LoadedDelegate);

        private async void LoadedDelegate(Frame obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                await UiThread.Dispatcher.InvokeAsync(() =>
                {
                    if (!_regionManager.Regions.ContainsRegionWithName("PackageSortingRegion"))
                    {
                        //创建区域(用于视觉树以外控件)
                        RegionManager.SetRegionName(obj, NavigationRegions.PackageSorting.Name);
                        RegionManager.SetRegionManager(obj, _regionManager);
                    }
                    _regionManager.Navigate(
                        new NavigationRequest(
                            NavigationRegions.PackageSorting,
                            NavigationDestinations.PackageExitDefinition));
                });
            }
        }

        /// <summary>
        /// 点击事件
        /// </summary>
        public ICommand ClickCommand => new DelegateCommand<MenuItemInfoModel>(MenuClickDelegate);

        private async void MenuClickDelegate(MenuItemInfoModel obj)
        {
            await UiThread.Dispatcher.InvokeAsync(() =>
            {
                if (!obj.PageClassName.Equals(string.Empty))
                {
                    foreach (var item in PackageSortingMenuItems)
                    {
                        item.IsSelected = false;
                    }

                    obj.IsSelected = true;

                    _regionManager.Navigate(
                        NavigationRequest.To(NavigationRegions.PackageSorting, obj.PageClassName));
                }
            }, DispatcherPriority.Background);
        }
    }
}
