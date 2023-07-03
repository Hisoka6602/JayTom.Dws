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
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.ViewModels.Pages {
    public class SettingsPageModel : BindableBase {
        private readonly IRegionManager _regionManager;

        private ObservableCollection<MenuItemInfoModel> _menuItems;

        public SettingsPageModel(IRegionManager regionManager) {
            _regionManager = regionManager;
            _menuItems = new()
            {
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe69d",
                        IconSize = 22,
                    },
                    Title = "插件信息",
                    Description = "灵活下载/组合插件插件",
                    ClickCommand = ClickCommand,
                    PageClassName = "aa"
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
                    PageClassName = "aa"
                },
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe664",
                        IconSize = 22
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

        private async void MenuClickDelegate(MenuItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                if (!obj.PageClassName.Equals(string.Empty)) {
                    foreach (var item in MenuItems) {
                        item.IsSelected = false;
                    }
                    obj.IsSelected = true;
                    //如果找到前一个按钮则设置，前一个按钮的右下圆角为10，
                    //如果找到后一个按钮则设置后一个按钮的右上圆角为10，
                    //把背景色设置成和大背景色一样的颜色

                    //_regionManager.Regions["ContentRegion"].RequestNavigate(obj.NavigationPage);
                }
            });
        }
    }
}