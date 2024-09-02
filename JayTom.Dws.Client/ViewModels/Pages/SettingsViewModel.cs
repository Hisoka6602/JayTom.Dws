using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Regions;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using System.Threading;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Domain.Dto.AppDto;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Domain.Repository.LocalConf;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class SettingsViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        private readonly IConfigRepository _configRepository;
        private Frame? _frame;
        private ObservableCollection<MenuItemInfoModel> _menuItems;
        private double _listBoxMaxHeight = 900;
        private PassWordSettingsDto? _passWordSettingsDto;

        public SettingsViewModel(IRegionManager regionManager, IConfigRepository configRepository) {
            _regionManager = regionManager;
            _configRepository = configRepository;
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
                    Description = Languages.Language.ResourceManager.GetString("数据信息管理")??string.Empty,
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
                    Description =Languages.Language.ResourceManager.GetString("相机管理配置")??string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "CameraConfigurationPage"
                },
                /*new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe664",
                        IconSize = 25
                    },
                    Title = Languages.Language.ResourceManager.GetString("ApiInterface")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("Api上传接口设置") ?? string.Empty,
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
                    Description = Languages.Language.ResourceManager.GetString("存图相关设置") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "SaveImageSettingsPage"
                },*/
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe6af",
                        IconSize = 30
                    },
                    Title = Languages.Language.ResourceManager.GetString("FilteringSettings")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("条码过滤相关设置") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "BarcodeFilterSettingsPage"
                },
                /*new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe71e",
                        IconSize = 26
                    },
                    Title = Languages.Language.ResourceManager.GetString("WeightSettings")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("磅秤和称重设置相关") ?? string.Empty,
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
                    Description = Languages.Language.ResourceManager.GetString("体积设置相关") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "VolumeSettingsPage"
                },*/
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe641",
                        IconSize = 30
                    },
                    //Title = Languages.Language.ResourceManager.GetString("OutputResults")??string.Empty,

                    Description = Languages.Language.ResourceManager.GetString("结果输出相关设置") ?? string.Empty,
                    Title = "数据输出",
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
                    //Title = Languages.Language.ResourceManager.GetString("InputContent")??string.Empty,
                    Description = Languages.Language.ResourceManager.GetString("内容输入相关设置") ?? string.Empty,
                    Title = "数据输入",
                    ClickCommand = ClickCommand,
                    PageClassName = "ContentInputSettingsPage"
                },
                /*new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe79f",
                        IconSize = 33
                    },
                    Title = "分拣设置"??string.Empty,
                    Description = "分拣相关设置" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "PackageSortingSettingsPage"
                },*/
                /*new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe91e",
                        IconSize = 25
                    },
                    Title = "组包设置"??string.Empty,
                    Description = "组包相关设置" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "CreatePackageSettingsPage"
                },*/
                /*new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe7a0",
                        IconSize = 25
                    },
                    Title = "Ocr设置"??string.Empty,
                    Description = "Ocr相关设置" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "OcrSettingsPage"
                },*/
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe944",
                        IconSize = 25
                    },
                    Title = "云端服务"??string.Empty,
                    Description = "云端服务相关" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "CloudServicePage"
                },
                /*new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe63e",
                        IconSize = 30
                    },
                    Title = "工作流"??string.Empty,
                    Description = "工作流相关设置" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "WorkflowSettingsPage"
                },*/
                new MenuItemInfoModel()
                {
                    IconFont = new IconInfoModel()
                    {
                        IconFont = "pack://application:,,,/Fonts/#iconfont",
                        IconCode = "\xe713",
                        IconSize = 30
                    },
                    Title = "程序设置"??string.Empty,
                    Description = "程序设置" ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "AppSettingsPage"
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
                    Description = Languages.Language.ResourceManager.GetString("运行日志\\设备日志\\通讯日志管理") ?? string.Empty,
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
                    Description = Languages.Language.ResourceManager.GetString("释放缓存/空间的方案") ?? string.Empty,
                    ClickCommand = ClickCommand,
                    PageClassName = "CacheClearSettingsPage"
                },
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                await Task.Delay(100);
                if (item is WindowsAction info && _frame is not null) {
                    if (info.Type == WindowsActionType.Maximize) {
                        ListBoxMaxHeight = _frame.ActualHeight;
                    }
                    else {
                        ListBoxMaxHeight = 900;
                    }
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async settings => {
                if (settings is SettingsChangedEvent { SettingsName: "PassWordSettings" }) {
                    _passWordSettingsDto = await _configRepository.FirstOrDefaultEntity<PassWordSettingsDto>("PassWordSettings") ?? new PassWordSettingsDto();
                }
            });
        }

        //MenuItems
        public ObservableCollection<MenuItemInfoModel> MenuItems {
            get => _menuItems;
            set => SetProperty(ref _menuItems, value);
        }

        public double ListBoxMaxHeight {
            get => _listBoxMaxHeight;
            set => SetProperty(ref _listBoxMaxHeight, value);
        }

        /// <summary>
        /// 点击事件
        /// </summary>
        public ICommand ClickCommand => new DelegateCommand<MenuItemInfoModel>(MenuClickDelegate);

        /// <summary>
        /// 窗口加载完成
        /// </summary>
        public ICommand LoadedCommand => new DelegateCommand<Frame>(LoadedDelegate);

        private async void LoadedDelegate(Frame obj) {
            _passWordSettingsDto ??= await _configRepository.FirstOrDefaultEntity<PassWordSettingsDto>("PassWordSettings") ?? new PassWordSettingsDto();
            await Application.Current.Dispatcher.InvokeAsync(() => {
                //加载loading
                _frame = obj;
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

        private async void MenuClickDelegate(MenuItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                //判断是否使用密码

                //加载loading

                //弹出密码框
                if (_passWordSettingsDto?.IsUsePasswordProtection == true && AppContext.GetData("IsValidationPassed") is not true &&
                    _passWordSettingsDto?.PasswordProtectionModuleItems
                        ?.Any(a => a.IsProtected && a.PageClassName.Equals(obj.PageClassName)) == true
                    ) {
                    var passwordValidationDialog = new PasswordValidationDialog();
                    if (passwordValidationDialog.DataContext is PasswordValidationDialogViewModel viewModel) {
                        viewModel.Identifier = "SettingDialog";
                        await DialogHost.Show(passwordValidationDialog, viewModel.Identifier);

                        if (!viewModel.IsValidationPassed) {
                            return;
                        }
                    }
                }

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
            });
        }
    }
}