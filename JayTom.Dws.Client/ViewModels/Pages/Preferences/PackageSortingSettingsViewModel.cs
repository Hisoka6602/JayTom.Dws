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

    public class PackageSortingSettingsViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        private ObservableCollection<MenuItemInfoModel> _packageSortingMenuItems;
        private static bool _isLoaded;

        public PackageSortingSettingsViewModel(IRegionManager regionManager) {
            _regionManager = regionManager;
            _packageSortingMenuItems = new ObservableCollection<MenuItemInfoModel>()
            {
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
            };
        }

        public ObservableCollection<MenuItemInfoModel> PackageSortingMenuItems {
            get => _packageSortingMenuItems;
            set => SetProperty(ref _packageSortingMenuItems, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<Frame>(LoadedDelegate);
        }

        private async void LoadedDelegate(Frame obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    if (!_regionManager.Regions.ContainsRegionWithName("PackageSortingRegion")) {
                        //创建区域(用于视觉树以外控件)
                        RegionManager.SetRegionName(obj, "PackageSortingRegion");
                        RegionManager.SetRegionManager(obj, _regionManager);
                    }
                    _regionManager.Regions["PackageSortingRegion"].RequestNavigate("PackageExitDefinitionPage");
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
                    foreach (var item in PackageSortingMenuItems) {
                        item.IsSelected = false;
                    }

                    obj.IsSelected = true;

                    _regionManager?.Regions?["PackageSortingRegion"]
                        ?.RequestNavigate(new Uri(obj.PageClassName, UriKind.Relative));
                }
            }, DispatcherPriority.Background);
        }
    }
}