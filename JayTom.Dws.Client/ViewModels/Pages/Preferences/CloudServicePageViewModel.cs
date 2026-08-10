using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Regions;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences
{

    public class CloudServicePageViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private static bool _isLoaded;
        private ObservableCollection<MenuItemInfoModel> _cloudServiceMenuItems = new();

        public CloudServicePageViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            CloudServiceMenuItems = new ObservableCollection<MenuItemInfoModel>()
            {
                new()
                {
                    Title = "数据中台",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe8b5",
                        IconSize = 25
                    },
                    IsSelected = true,
                    Description = "视频云",
                    PageClassName = "CloudVideoPage",
                    ClickCommand = ClickCommand
                },
                new()
                {
                    Title = "录像NVR",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe9ef",
                        IconSize = 25
                    },
                    IsSelected = false,
                    Description = "录像NVR",
                    PageClassName = "NetworkVideoRecorderPage",
                    ClickCommand = ClickCommand
                },
                /*new()
                {
                    Title = "云端数据",
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe98c",
                        IconSize = 27
                    },
                    Description = "云端数据",
                    PageClassName = "CloudDataPage",
                    ClickCommand = ClickCommand
                },*/
            };
        }

        public ObservableCollection<MenuItemInfoModel> CloudServiceMenuItems
        {
            get => _cloudServiceMenuItems;
            set => SetProperty(ref _cloudServiceMenuItems, value);
        }

        public ICommand LoadedCommand => new DelegateCommand<Frame>(LoadedDelegate);

        private async void LoadedDelegate(Frame obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!_regionManager.Regions.ContainsRegionWithName("CloudServiceRegion"))
                    {
                        //创建区域(用于视觉树以外控件)
                        RegionManager.SetRegionName(obj, "CloudServiceRegion");
                        RegionManager.SetRegionManager(obj, _regionManager);
                    }
                    _regionManager.Regions["CloudServiceRegion"].RequestNavigate("CloudVideoPage");
                });
            }
        }

        /// <summary>
        /// 点击事件
        /// </summary>
        public ICommand ClickCommand => new DelegateCommand<MenuItemInfoModel>(MenuClickDelegate);

        private async void MenuClickDelegate(MenuItemInfoModel obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (!obj.PageClassName.Equals(string.Empty))
                {
                    foreach (var item in CloudServiceMenuItems)
                    {
                        item.IsSelected = false;
                    }

                    obj.IsSelected = true;

                    _regionManager?.Regions?["CloudServiceRegion"]
                        ?.RequestNavigate(new Uri(obj.PageClassName, UriKind.Relative));
                }
            }, DispatcherPriority.Background);
        }
    }
}